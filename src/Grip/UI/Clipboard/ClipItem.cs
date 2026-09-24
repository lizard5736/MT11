using System.IO;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Grip.Core.Clipboard;
using Grip.Core.Localization;
using Grip.Services;

namespace Grip.UI.Clipboard;

/// <summary>What a history row shows for one entry.</summary>
public sealed partial class ClipItem : ObservableObject
{
    public ClipEntry Entry { get; }

    public ClipItem(ClipEntry entry, int index = -1)
    {
        Entry = entry;
        Index = index;
    }

    public int Index { get; }

    public string? Shortcut => Index is >= 0 and < 9 ? $"Ctrl+{Index + 1}" : null;

    public bool IsPinned => Entry.Pinned;

    public void NotifyPinned() => OnPropertyChanged(nameof(IsPinned));

    public string KindIcon => Entry.Kind switch
    {
        ClipKind.Link => "Link",
        ClipKind.Image => "Image",
        ClipKind.Files => Entry.Files is { Count: 1 } files && Directory.Exists(files[0]) ? "Folder" : "Document",
        _ => "TextT",
    };

    public string Title => Entry.Kind switch
    {
        ClipKind.Image => L.F("clipboard.image", Entry.ImageWidth, Entry.ImageHeight),
        ClipKind.Files => Entry.Files is { Count: > 0 } files
            ? files.Count == 1 ? Path.GetFileName(files[0].TrimEnd('\\')) : $"{Path.GetFileName(files[0].TrimEnd('\\'))} +{files.Count - 1}"
            : "",
        _ => ClipText.Preview(Entry.Text, 300),
    };

    public string Meta
    {
        get
        {
            var parts = new List<string>(3);
            var loc = Localizer.Instance;
            if (Entry.Kind == ClipKind.Files && Entry.Files is { Count: > 1 })
                parts.Add(loc.Count("clipboard.fileCount", Entry.Files.Count));
            else if (Entry.Kind is ClipKind.Text && Entry.Text is { Length: > 300 })
                parts.Add(loc.Count("clipboard.chars", Entry.Text.Length));
            else if (Entry.Kind == ClipKind.Link && Uri.TryCreate(Entry.Text?.Trim(), UriKind.Absolute, out var uri))
                parts.Add(uri.Host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? uri.Host[4..] : uri.Host);
            if (!string.IsNullOrEmpty(Entry.SourceApp)) parts.Add(Path.GetFileNameWithoutExtension(Entry.SourceApp));
            parts.Add(loc.Ago(Entry.LastUsed, DateTimeOffset.Now));
            return string.Join("  ·  ", parts);
        }
    }

    public ImageSource? Thumbnail => Entry.Kind == ClipKind.Image ? ClipboardThumbnails.Get(Entry, 120) : null;

    public bool HasThumbnail => Entry.Kind == ClipKind.Image;

    public string? ColorHex => Entry.ColorHex;

    public bool HasColor => ColorHex != null;

    public bool ShowIcon => !HasThumbnail && !HasColor;

    /// <summary>The full text for the preview pane.</summary>
    public string FullText => Entry.Kind switch
    {
        ClipKind.Files => string.Join(Environment.NewLine, Entry.Files ?? new List<string>()),
        ClipKind.Image => "",
        _ => Entry.Text is { Length: > 20000 } t ? t[..20000] + "…" : Entry.Text ?? "",
    };

    public ImageSource? LargePreview => Entry.Kind == ClipKind.Image ? ClipboardThumbnails.Get(Entry, 640) : null;
}
