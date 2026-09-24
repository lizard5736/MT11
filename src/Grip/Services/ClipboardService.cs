using System.Collections.Specialized;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Grip.Core.Clipboard;
using Grip.Core.Features;
using Grip.Core.Text;
using Grip.Interop;
using Grip.UI;
using Grip.UI.Common;
using Microsoft.Win32;
using static Grip.Interop.NativeMethods;

namespace Grip.Services;

/// <summary>
/// Everything clipboard: listens for changes (event driven, no polling),
/// keeps the history, cleans links, clears the clipboard on a timer or when
/// the PC locks or sleeps, and pastes entries back into the app you came from.
/// </summary>
public sealed class ClipboardService : IDisposable
{
    private readonly SettingsService _settings;
    private readonly MessageWindow _window;
    private readonly DispatcherTimer _readTimer = new() { Interval = TimeSpan.FromMilliseconds(80) };
    private readonly DispatcherTimer _saveTimer = new() { Interval = TimeSpan.FromSeconds(1.5) };
    private readonly DispatcherTimer _autoClearTimer = new();
    private readonly Queue<uint> _ownSequences = new();
    private bool _listening;
    private uint _autoClearSequence;
    private bool _loaded;

    public ClipboardHistory History { get; } = new();

    public event EventHandler? HistoryChanged;

    public ClipboardService(SettingsService settings, MessageWindow window)
    {
        _settings = settings;
        _window = window;
        _window.Message += OnMessage;
        _readTimer.Tick += (_, _) =>
        {
            _readTimer.Stop();
            ProcessClipboard();
        };
        _saveTimer.Tick += (_, _) =>
        {
            _saveTimer.Stop();
            SaveHistory();
        };
        _autoClearTimer.Tick += (_, _) =>
        {
            _autoClearTimer.Stop();
            if (GetClipboardSequenceNumber() == _autoClearSequence) ClearSystemClipboard(showHud: false);
        };
        History.Changed += (_, _) =>
        {
            HistoryChanged?.Invoke(this, EventArgs.Empty);
            _saveTimer.Stop();
            _saveTimer.Start();
        };
        History.Removed += (_, removed) => DeleteImages(removed);
        SystemEvents.SessionSwitch += OnSessionSwitch;
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
    }

    private bool HistoryInstalled => _settings.Current.IsInstalled(FeatureIds.ClipboardHistory);
    private bool HistoryOn => HistoryInstalled && _settings.Current.Clipboard.HistoryEnabled;
    private bool AutoCleanOn => _settings.Current.IsInstalled(FeatureIds.UrlCleaner) && _settings.Current.UrlCleaner.AutoClean;
    private bool AutoClearOn => HistoryInstalled && _settings.Current.Clipboard.AutoClearEnabled;

    /// <summary>Starts or stops listening to match the current settings.</summary>
    public void ApplySettings()
    {
        var c = _settings.Current.Clipboard;
        History.Options.MaxItems = c.MaxItems;
        History.Options.RetentionDays = c.RetentionDays;
        if (!_loaded && HistoryInstalled)
        {
            LoadHistory();
            _loaded = true;
        }
        History.Trim(DateTimeOffset.Now);

        bool needListener = HistoryOn || AutoCleanOn || AutoClearOn;
        if (needListener && !_listening)
        {
            _listening = AddClipboardFormatListener(_window.Handle);
            if (!_listening) Log.Warn("AddClipboardFormatListener failed: " + Marshal.GetLastWin32Error());
            else Log.Info("Clipboard listener on");
        }
        else if (!needListener && _listening)
        {
            RemoveClipboardFormatListener(_window.Handle);
            _listening = false;
            Log.Info("Clipboard listener off");
        }
        if (!AutoClearOn) _autoClearTimer.Stop();
    }

    /// <summary>What the Access page lists: is the clipboard being watched right now?</summary>
    public bool IsListening => _listening;

    private bool OnMessage(int msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg != WM_CLIPBOARDUPDATE) return false;
        _readTimer.Stop();
        _readTimer.Start();
        return true;
    }

    // ---------- reading ----------

    private void ProcessClipboard()
    {
        uint sequence = GetClipboardSequenceNumber();
        if (_ownSequences.Contains(sequence)) return;

        IDataObject? data = TryGetDataObject();
        if (data == null) return;

        if (IsMarkedPrivate(data)) return;

        var owner = GetClipboardOwner();
        var source = ProcessInfo.ExeNameForWindow(owner) ?? ProcessInfo.ExeNameForWindow(GetForegroundWindow());
        if (source != null && _settings.Current.Clipboard.IgnoredApps.Any(a => a.Equals(source, StringComparison.OrdinalIgnoreCase)))
            return;

        var now = DateTimeOffset.Now;
        var c = _settings.Current.Clipboard;
        try
        {
            if (data.GetDataPresent(DataFormats.FileDrop) && data.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
            {
                if (HistoryOn && c.SaveFiles) History.AddOrPromote(ClipEntry.FromFiles(files, source, now), now);
            }
            else if (data.GetDataPresent(DataFormats.UnicodeText) && data.GetData(DataFormats.UnicodeText) is string text)
            {
                if (!string.IsNullOrWhiteSpace(text) && text.Length <= ClipText.MaxTextLength)
                {
                    text = MaybeCleanLink(text);
                    if (HistoryOn) History.AddOrPromote(ClipEntry.FromText(text, source, now), now);
                }
            }
            else if (HistoryOn && c.SaveImages && ClipboardImages.TryRead(data) is { } image)
            {
                StoreImage(image, source, now);
            }
        }
        catch (Exception ex) when (ex is COMException or ExternalException or OutOfMemoryException or ArgumentException)
        {
            Log.Warn("Clipboard read failed: " + ex.Message);
        }

        if (AutoClearOn)
        {
            _autoClearSequence = GetClipboardSequenceNumber();
            _autoClearTimer.Interval = TimeSpan.FromSeconds(c.AutoClearSeconds);
            _autoClearTimer.Stop();
            _autoClearTimer.Start();
        }
    }

    private string MaybeCleanLink(string text)
    {
        if (!AutoCleanOn || !UrlCleaner.IsHttpUrl(text)) return text;
        var result = UrlCleaner.Clean(text, CleanerOptions());
        if (!result.Changed) return text;
        if (SetText(result.Url))
        {
            if (_settings.Current.UrlCleaner.ShowHud) ShowCleanedHud(result);
            return result.Url;
        }
        return text;
    }

    public UrlCleanerOptions CleanerOptions() => new()
    {
        UnwrapRedirects = _settings.Current.UrlCleaner.UnwrapRedirects,
        CleanSearchLinks = _settings.Current.UrlCleaner.CleanSearchLinks,
        ExtraParameters = _settings.Current.UrlCleaner.ExtraParameters,
        SkipDomains = _settings.Current.UrlCleaner.SkipDomains,
    };

    private static void ShowCleanedHud(UrlCleanResult result)
    {
        var text = result.RemovedCount > 0 ? L.F("url.cleaned", result.RemovedCount) : L.S("url.unwrapped");
        App.Services.Hud.Show(text, "Link", HudTone.Success);
    }

    /// <summary>Private content: password managers set these formats to stay out of history.</summary>
    private static bool IsMarkedPrivate(IDataObject data)
    {
        try
        {
            if (data.GetDataPresent("ExcludeClipboardContentFromMonitorProcessing")) return true;
            if (data.GetDataPresent("Clipboard Viewer Ignore")) return true;
            if (data.GetDataPresent("CanIncludeInClipboardHistory")
                && data.GetData("CanIncludeInClipboardHistory") is MemoryStream flag && flag.Length >= 4)
            {
                var bytes = flag.ToArray();
                return BitConverter.ToInt32(bytes, 0) == 0;
            }
        }
        catch (COMException) { }
        return false;
    }

    private static IDataObject? TryGetDataObject()
    {
        for (int attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                return System.Windows.Clipboard.GetDataObject();
            }
            catch (Exception ex) when (ex is COMException or ExternalException)
            {
                Thread.Sleep(25);
            }
        }
        return null;
    }

    private void StoreImage(BitmapSource image, string? source, DateTimeOffset now)
    {
        var bgra = image.Format == PixelFormats.Bgra32 ? image : new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);
        int w = bgra.PixelWidth, h = bgra.PixelHeight;
        if (w <= 0 || h <= 0 || (long)w * h > 50_000_000) return;
        int stride = w * 4;
        var pixels = new byte[stride * h];
        bgra.CopyPixels(pixels, stride, 0);
        var hash = ClipHash.OfBytes(pixels);
        var fileName = hash[..32] + ".png";
        var path = Path.Combine(AppPaths.ClipboardImagesDir, fileName);

        // Encoding a large screenshot takes a moment; do it off the UI thread.
        Task.Run(() =>
        {
            try
            {
                if (!File.Exists(path))
                {
                    Directory.CreateDirectory(AppPaths.ClipboardImagesDir);
                    var bitmap = BitmapSource.Create(w, h, 96, 96, PixelFormats.Bgra32, null, pixels, stride);
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    using var fs = File.Create(path);
                    encoder.Save(fs);
                }
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Log.Warn("Could not store clipboard image: " + ex.Message);
                return false;
            }
        }).ContinueWith(t =>
        {
            if (t.Result) History.AddOrPromote(ClipEntry.FromImage(fileName, w, h, hash, source, now), now);
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }

    // ---------- writing ----------

    /// <summary>Remembers a write of ours so the listener doesn't treat it as a new copy.</summary>
    private void MarkOwnWrite()
    {
        _ownSequences.Enqueue(GetClipboardSequenceNumber());
        while (_ownSequences.Count > 16) _ownSequences.Dequeue();
    }

    private bool TrySet(Action set)
    {
        for (int attempt = 0; attempt < 6; attempt++)
        {
            try
            {
                set();
                MarkOwnWrite();
                return true;
            }
            catch (Exception ex) when (ex is COMException or ExternalException)
            {
                Thread.Sleep(30);
            }
        }
        Log.Warn("Clipboard stayed locked by another app");
        return false;
    }

    public bool SetText(string text)
    {
        var data = new DataObject();
        data.SetText(text, TextDataFormat.UnicodeText);
        return TrySet(() => System.Windows.Clipboard.SetDataObject(data, true));
    }

    /// <summary>Puts an entry back on the clipboard; <paramref name="plain"/> drops everything but text.</summary>
    public bool SetEntry(ClipEntry entry, bool plain = false)
    {
        switch (entry.Kind)
        {
            case ClipKind.Text:
            case ClipKind.Link:
                return entry.Text != null && SetText(entry.Text);

            case ClipKind.Files:
            {
                var existing = (entry.Files ?? new List<string>()).Where(f => File.Exists(f) || Directory.Exists(f)).ToList();
                if (existing.Count < (entry.Files?.Count ?? 0))
                    App.Services.Hud.Show(L.S("clipboard.missingFiles"), "Warning", HudTone.Rec);
                if (existing.Count == 0) return false;
                if (plain) return SetText(string.Join(Environment.NewLine, existing));
                var list = new StringCollection();
                list.AddRange(existing.ToArray());
                var data = new DataObject();
                data.SetFileDropList(list);
                return TrySet(() => System.Windows.Clipboard.SetDataObject(data, true));
            }

            case ClipKind.Image:
            {
                var path = ImagePath(entry);
                if (path == null || !File.Exists(path)) return false;
                var bytes = File.ReadAllBytes(path);
                var decoder = new PngBitmapDecoder(new MemoryStream(bytes), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                var data = new DataObject();
                data.SetImage(decoder.Frames[0]);
                // PNG keeps transparency for apps that read it (browsers, Office, Figma).
                data.SetData("PNG", new MemoryStream(bytes));
                return TrySet(() => System.Windows.Clipboard.SetDataObject(data, true));
            }
        }
        return false;
    }

    /// <summary>Copies an entry and, when asked, pastes it into <paramref name="target"/>.</summary>
    public async Task PasteAsync(ClipEntry entry, bool plain, IntPtr target)
    {
        if (!SetEntry(entry, plain)) return;
        History.Promote(entry.Id, DateTimeOffset.Now);
        if (target == IntPtr.Zero || !IsWindow(target) || !_settings.Current.Clipboard.PasteOnSelect)
        {
            App.Services.Hud.Show(L.S("common.copied"), "Copy", HudTone.Success);
            return;
        }
        if (ProcessInfo.IsElevated(target) && !ProcessInfo.CurrentIsElevated)
        {
            App.Services.Hud.Show(L.S("clipboard.pasteFailed"), "Warning", HudTone.Rec);
            return;
        }
        await Focus(target);
        InputSender.SendCtrlV();
    }

    /// <summary>Puts text on the clipboard and pastes it into <paramref name="target"/> (emoji, snippets).</summary>
    public async Task PasteTextAsync(string text, IntPtr target)
    {
        if (!SetText(text)) return;
        if (target == IntPtr.Zero || !IsWindow(target) || (ProcessInfo.IsElevated(target) && !ProcessInfo.CurrentIsElevated))
        {
            App.Services.Hud.Show(L.S("common.copied") + ": " + text, "Copy", HudTone.Success);
            return;
        }
        await Focus(target);
        InputSender.SendCtrlV();
    }

    private static async Task Focus(IntPtr target)
    {
        WindowStyling.ForceForeground(target);
        for (int i = 0; i < 15 && GetForegroundWindow() != target; i++)
            await Task.Delay(20);
        await Task.Delay(30);
    }

    /// <summary>
    /// The paste-as-plain-text shortcut: swaps in the bare text, presses
    /// Ctrl+V in the app in front, then puts the original content back.
    /// </summary>
    public async Task PastePlainAsync()
    {
        var original = TryGetDataObject();
        if (original == null) return;
        string? text = null;
        try
        {
            if (original.GetDataPresent(DataFormats.UnicodeText)) text = original.GetData(DataFormats.UnicodeText) as string;
            else if (original.GetDataPresent(DataFormats.FileDrop) && original.GetData(DataFormats.FileDrop) is string[] files)
                text = string.Join(Environment.NewLine, files);
        }
        catch (COMException) { }

        if (string.IsNullOrEmpty(text))
        {
            App.Services.Hud.Show(L.S("clipboard.noText"), "TextClearFormatting", HudTone.Neutral);
            return;
        }

        var snapshot = Snapshot(original);
        var target = GetForegroundWindow();
        if (ProcessInfo.IsElevated(target) && !ProcessInfo.CurrentIsElevated)
        {
            App.Services.Hud.Show(L.S("clipboard.pasteFailed"), "Warning", HudTone.Rec);
            return;
        }
        if (!SetText(text)) return;
        InputSender.SendCtrlV();
        // Give the app time to read the clipboard before restoring it.
        await Task.Delay(350);
        if (snapshot != null) TrySet(() => System.Windows.Clipboard.SetDataObject(snapshot, true));
    }

    /// <summary>Copies every format we can read so the original clipboard can be restored.</summary>
    private static DataObject? Snapshot(IDataObject source)
    {
        var copy = new DataObject();
        int kept = 0;
        string[] formats;
        try { formats = source.GetFormats(false); }
        catch (COMException) { return null; }
        foreach (var format in formats)
        {
            try
            {
                var value = source.GetData(format, false);
                // Only plain data survives a round trip reliably; skip live objects.
                if (value is not (string or string[] or MemoryStream or BitmapSource)) continue;
                copy.SetData(format, value, false);
                kept++;
            }
            catch (Exception ex) when (ex is COMException or ExternalException or OutOfMemoryException or InvalidOperationException) { }
        }
        return kept > 0 ? copy : null;
    }

    public void ClearSystemClipboard(bool showHud)
    {
        TrySet(System.Windows.Clipboard.Clear);
        if (showHud) App.Services.Hud.Show(L.S("clipboard.cleared"), "Broom", HudTone.Neutral);
    }

    /// <summary>The "Clean the link on the clipboard" action.</summary>
    public void CleanLinkOnClipboard()
    {
        string? text = null;
        try
        {
            if (System.Windows.Clipboard.ContainsText()) text = System.Windows.Clipboard.GetText();
        }
        catch (Exception ex) when (ex is COMException or ExternalException) { }

        if (text == null || !UrlCleaner.IsHttpUrl(text))
        {
            App.Services.Hud.Show(L.S("url.noLink"), "Link", HudTone.Neutral);
            return;
        }
        var result = UrlCleaner.Clean(text, CleanerOptions());
        if (!result.Changed)
        {
            App.Services.Hud.Show(L.S("url.nothing"), "Link", HudTone.Neutral);
            return;
        }
        if (SetText(result.Url))
        {
            if (HistoryOn) History.AddOrPromote(ClipEntry.FromText(result.Url, "Grip", DateTimeOffset.Now), DateTimeOffset.Now);
            ShowCleanedHud(result);
        }
    }

    // ---------- lock and sleep ----------

    private void OnSessionSwitch(object? sender, SessionSwitchEventArgs e)
    {
        if (e.Reason == SessionSwitchReason.SessionLock && HistoryInstalled && _settings.Current.Clipboard.ClearOnLock)
            Application.Current?.Dispatcher.BeginInvoke(() => ClearSystemClipboard(showHud: false));
    }

    private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Suspend && HistoryInstalled && _settings.Current.Clipboard.ClearOnSleep)
            Application.Current?.Dispatcher.Invoke(() => ClearSystemClipboard(showHud: false));
    }

    // ---------- storage ----------

    public static string? ImagePath(ClipEntry entry) =>
        entry.ImageFile == null ? null : Path.Combine(AppPaths.ClipboardImagesDir, entry.ImageFile);

    private void LoadHistory()
    {
        if (!_settings.Current.Clipboard.KeepAfterRestart) return;
        try
        {
            if (File.Exists(AppPaths.ClipboardFile))
                History.Load(File.ReadAllText(AppPaths.ClipboardFile));
            RemoveOrphanImages();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or System.Text.Json.JsonException)
        {
            Log.Warn("Clipboard history could not be read: " + ex.Message);
        }
    }

    public void SaveHistory()
    {
        try
        {
            Directory.CreateDirectory(AppPaths.ClipboardDir);
            if (_settings.Current.Clipboard.KeepAfterRestart && HistoryInstalled)
                Core.Settings.SettingsStore.WriteAtomically(AppPaths.ClipboardFile, History.Serialize());
            else if (File.Exists(AppPaths.ClipboardFile))
                File.Delete(AppPaths.ClipboardFile);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warn("Clipboard history could not be saved: " + ex.Message);
        }
    }

    private void DeleteImages(IEnumerable<ClipEntry> removed)
    {
        foreach (var entry in removed)
        {
            if (entry.ImageFile == null) continue;
            if (History.Items.Any(e => e.ImageFile == entry.ImageFile)) continue;
            try
            {
                File.Delete(Path.Combine(AppPaths.ClipboardImagesDir, entry.ImageFile));
                ClipboardThumbnails.Forget(entry.ImageFile);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }
    }

    private void RemoveOrphanImages()
    {
        if (!Directory.Exists(AppPaths.ClipboardImagesDir)) return;
        var used = History.Items.Where(e => e.ImageFile != null).Select(e => e.ImageFile!).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.GetFiles(AppPaths.ClipboardImagesDir, "*.png"))
        {
            if (used.Contains(Path.GetFileName(file))) continue;
            try { File.Delete(file); }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        }
    }

    public void Dispose()
    {
        SystemEvents.SessionSwitch -= OnSessionSwitch;
        SystemEvents.PowerModeChanged -= OnPowerModeChanged;
        if (_listening) RemoveClipboardFormatListener(_window.Handle);
        _window.Message -= OnMessage;
        _saveTimer.Stop();
        SaveHistory();
    }
}

/// <summary>Reads images from clipboard data, trying the formats that keep the most detail first.</summary>
public static class ClipboardImages
{
    public static BitmapSource? TryRead(IDataObject data)
    {
        try
        {
            if (data.GetDataPresent("PNG") && data.GetData("PNG") is MemoryStream png)
            {
                var decoder = new PngBitmapDecoder(png, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                return decoder.Frames[0];
            }
            if (data.GetDataPresent(DataFormats.Dib) && data.GetData(DataFormats.Dib) is MemoryStream dib
                && FromDib(dib.ToArray()) is { } fromDib)
                return fromDib;
            if (data.GetDataPresent(DataFormats.Bitmap) && data.GetData(DataFormats.Bitmap) is BitmapSource bitmap)
                return bitmap;
        }
        catch (Exception ex) when (ex is COMException or NotSupportedException or FileFormatException or ArgumentException or OverflowException) { }
        return null;
    }

    /// <summary>Wraps a packed DIB in a BMP file header so WPF can decode it.</summary>
    public static BitmapSource? FromDib(byte[] dib)
    {
        if (dib.Length < 40) return null;
        int headerSize = BitConverter.ToInt32(dib, 0);
        if (headerSize < 40 || headerSize > dib.Length) return null;
        short bitCount = BitConverter.ToInt16(dib, 14);
        int compression = BitConverter.ToInt32(dib, 16);
        int colorsUsed = BitConverter.ToInt32(dib, 32);
        int masks = compression == 3 && headerSize == 40 ? 12 : 0;
        int palette = (colorsUsed != 0 ? colorsUsed : bitCount <= 8 ? 1 << bitCount : 0) * 4;
        int offBits = 14 + headerSize + masks + palette;

        var file = new byte[14 + dib.Length];
        file[0] = (byte)'B';
        file[1] = (byte)'M';
        BitConverter.GetBytes(file.Length).CopyTo(file, 2);
        BitConverter.GetBytes(offBits).CopyTo(file, 10);
        Buffer.BlockCopy(dib, 0, file, 14, dib.Length);

        var decoder = new BmpBitmapDecoder(new MemoryStream(file), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        BitmapSource frame = decoder.Frames[0];

        // Many apps write 32-bit DIBs with an all-zero alpha channel; treat those as opaque.
        if (frame.Format == PixelFormats.Bgra32 && AlphaIsEmpty(frame))
            frame = new FormatConvertedBitmap(frame, PixelFormats.Bgr32, null, 0);
        return frame;
    }

    private static bool AlphaIsEmpty(BitmapSource frame)
    {
        int stride = frame.PixelWidth * 4;
        var pixels = new byte[stride * frame.PixelHeight];
        frame.CopyPixels(pixels, stride, 0);
        for (int i = 3; i < pixels.Length; i += 4)
            if (pixels[i] != 0) return false;
        return true;
    }
}

/// <summary>Small decoded previews of history images, cached by file name.</summary>
public static class ClipboardThumbnails
{
    private static readonly Dictionary<string, BitmapSource> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static BitmapSource? Get(ClipEntry entry, int decodeWidth = 160)
    {
        if (entry.ImageFile == null) return null;
        var key = entry.ImageFile + "@" + decodeWidth;
        if (Cache.TryGetValue(key, out var cached)) return cached;
        var path = ClipboardService.ImagePath(entry);
        if (path == null || !File.Exists(path)) return null;
        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.DecodePixelWidth = Math.Min(decodeWidth, Math.Max(1, entry.ImageWidth));
            image.UriSource = new Uri(path);
            image.EndInit();
            image.Freeze();
            if (Cache.Count > 400) Cache.Clear();
            Cache[key] = image;
            return image;
        }
        catch (Exception ex) when (ex is IOException or NotSupportedException or FileFormatException)
        {
            return null;
        }
    }

    public static void Forget(string imageFile)
    {
        foreach (var key in Cache.Keys.Where(k => k.StartsWith(imageFile + "@", StringComparison.OrdinalIgnoreCase)).ToList())
            Cache.Remove(key);
    }
}
