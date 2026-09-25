using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Grip.Core.Clipboard;
using Grip.Core.Radial;
using Grip.Core.Settings;
using Grip.Services;
using Grip.UI.Clipboard;
using Grip.UI.Common;
using Grip.UI.CommandBar;
using Grip.UI.Notes;
using Grip.UI.Panel;
using Grip.UI.Radial;
using Grip.UI.Settings;
using Grip.UI.Shelf;

namespace Grip.UI;

/// <summary>
/// Developer tool: "Grip.exe --render-previews folder" draws every screen
/// with sample data into PNG files, without touching the real settings.
/// Used for design reviews and to catch layout problems before release.
/// </summary>
public static class PreviewRenderer
{
    private const double Scale = 1.5;

    public static void RenderAll(string dir)
    {
        Directory.CreateDirectory(dir);
        var log = new List<string>();
        void Try(string name, Action render)
        {
            try
            {
                render();
                log.Add("ok   " + name);
            }
            catch (Exception ex)
            {
                log.Add($"FAIL {name}: {ex}");
            }
        }

        var services = App.Services;
        services.Settings.Current.General.Language = AppLanguage.Russian;
        LanguageService.Apply(AppLanguage.Russian);
        Seed(services);

        foreach (var theme in new[] { AppTheme.Dark, AppTheme.Light })
        {
            services.Settings.Current.General.Theme = theme;
            services.Theme.Apply(theme, force: true);
            string suffix = theme == AppTheme.Dark ? "" : "-light";

            foreach (var section in new[] { "keepAwake", "clipboard", "utilities", "controls", "toggles", "monitor" })
            {
                if (theme == AppTheme.Light && section != "keepAwake" && section != "clipboard") continue;
                Try($"panel-{section}{suffix}", () =>
                {
                    services.Settings.Current.Panel.Layout = PanelLayoutMode.Tabs;
                    services.Settings.Current.Panel.LastSection = section;
                    var window = new FlyoutWindow { IsPreview = true };
                    window.Rebuild();
                    RenderWindow(window, 392, null, Path.Combine(dir, $"panel-{section}{suffix}.png"), refresh: true);
                });
            }
            if (theme == AppTheme.Light) goto settingsLight;

            Try("panel-list", () =>
            {
                services.Settings.Current.Panel.Layout = PanelLayoutMode.List;
                var window = new FlyoutWindow { IsPreview = true };
                window.Rebuild();
                RenderWindow(window, 392, null, Path.Combine(dir, "panel-list.png"), refresh: true);
                services.Settings.Current.Panel.Layout = PanelLayoutMode.Tabs;
            });

            Try("clipboard-window", () =>
            {
                var window = new ClipboardWindow { IsPreview = true };
                window.PreviewFill();
                RenderWindow(window, 760, 500, Path.Combine(dir, "clipboard-window.png"));
            });

            foreach (var (name, query) in new[] { ("calc", "15% от 2400 + 120"), ("units", "10 км в милях"), ("apps", "кал"), ("emoji", ":сердце"), ("layout", "ыуеештпы") })
            {
                Try("commandbar-" + name, () =>
                {
                    var window = new CommandBarWindow { IsPreview = true };
                    window.PreviewQuery(query);
                    RenderWindow(window, 660, null, Path.Combine(dir, $"commandbar-{name}.png"));
                });
            }

            Try("radial", () =>
            {
                var view = new RadialMenuView();
                view.SetItems(RadialProfile.CreateDefault().Items, false, RadialMenuView.RadiusFor(RadialSize.Medium));
                view.Highlight = 1;
                double side = RadialMenuView.RadiusFor(RadialSize.Medium) * 2 + 32;
                RenderElement(view, side, side, Path.Combine(dir, "radial.png"), transparent: true);
            });

            Try("shelf", () =>
            {
                var window = new ShelfWindow { IsPreview = true };
                window.AddFiles(new[] { @"C:\Projects\Film\edit_v12.prproj", @"C:\Projects\Film\Stills", @"C:\Projects\Film\poster_final.png" });
                window.AddTextItem("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
                window.AddTextItem("INT. КВАРТИРА — НОЧЬ");
                RenderWindow(window, 300, 360, Path.Combine(dir, "shelf.png"));
            });

            Try("notepad", () =>
            {
                var window = new NotepadWindow { IsPreview = true };
                RenderWindow(window, 420, 480, Path.Combine(dir, "notepad.png"));
            });

            Try("hud", () =>
            {
                var window = new HudWindow();
                window.ShowMessageForPreview(L.F("url.cleaned", 3), "Link", HudTone.Success);
                RenderWindow(window, 300, null, Path.Combine(dir, "hud.png"), transparent: true);
            });

            Try("onboarding", () => RenderWindow(new OnboardingWindow(), 640, null, Path.Combine(dir, "onboarding.png")));

        settingsLight:
            foreach (var page in theme == AppTheme.Dark
                         ? new[] { "general", "features", "access", "hotkeys", "clipboard", "links", "shelf", "commandBar", "radial", "toggles", "keepAwake", "about" }
                         : new[] { "features" })
            {
                Try($"settings-{page}{suffix}", () =>
                {
                    var window = new SettingsWindow();
                    window.PreviewPage(page);
                    RenderWindow(window, 1000, 760, Path.Combine(dir, $"settings-{page}{suffix}.png"));
                });
            }
        }

        // A few English screens to check that longer or shorter strings still fit.
        services.Settings.Current.General.Language = AppLanguage.English;
        LanguageService.Apply(AppLanguage.English);
        services.Theme.Apply(AppTheme.Dark, force: true);
        Try("panel-keepAwake-en", () =>
        {
            services.Settings.Current.Panel.LastSection = "keepAwake";
            var window = new FlyoutWindow { IsPreview = true };
            window.Rebuild();
            RenderWindow(window, 392, null, Path.Combine(dir, "panel-keepAwake-en.png"), refresh: true);
        });
        Try("settings-features-en", () =>
        {
            var window = new SettingsWindow();
            window.PreviewPage("features");
            RenderWindow(window, 1000, 760, Path.Combine(dir, "settings-features-en.png"));
        });

        File.WriteAllLines(Path.Combine(dir, "render-log.txt"), log);
    }

    /// <summary>Fills the preview profile with believable data.</summary>
    private static void Seed(AppServices services)
    {
        var s = services.Settings.Current;
        s.General.OnboardingCompleted = true;
        services.KeepAwake.Start(90);

        var history = services.Clipboard.History;
        var now = DateTimeOffset.Now;
        history.AddOrPromote(ClipEntry.FromFiles(new[] { @"C:\Projects\Film\edit_v12.prproj", @"C:\Projects\Film\sound_mix.wav", @"C:\Projects\Film\grade.drx" }, "explorer.exe", now.AddHours(-3)), now.AddHours(-3));
        history.AddOrPromote(ClipEntry.FromText("#FFB020", "Figma.exe", now.AddMinutes(-50)), now.AddMinutes(-50));
        var pinned = history.AddOrPromote(ClipEntry.FromText("Смена начинается в 7:30, сбор на площадке у павильона №2. Не забыть хлопушку и запасные карты памяти.", "Telegram.exe", now.AddMinutes(-35)), now.AddMinutes(-35));
        history.SetPinned(pinned.Id, true);
        history.AddOrPromote(ClipEntry.FromText("https://vimeo.com/123456789/review/final-cut", "chrome.exe", now.AddMinutes(-12)), now.AddMinutes(-12));
        history.AddOrPromote(ClipEntry.FromText("00:12:41:07", "Resolve.exe", now.AddMinutes(-4)), now.AddMinutes(-4));
        var image = SampleImage();
        if (image != null) history.AddOrPromote(image, now.AddMinutes(-2));
        history.AddOrPromote(ClipEntry.FromText("INT. КВАРТИРА — НОЧЬ. Героиня смотрит в окно, за стеклом — огни города.", "WINWORD.EXE", now.AddSeconds(-40)), now.AddSeconds(-40));

        var notes = services.Notepad.Notes;
        foreach (var blank in notes.Notes.ToList()) notes.Remove(blank.Id); // drop the auto-created empty note before seeding samples
        var n1 = notes.Add();
        notes.SetContent(n1.Id, "Идеи для монтажа\nПопробовать джамп-каты во второй сцене.\nПроверить синхрон звука на дубле 4.", now);
        var n2 = notes.Add();
        notes.SetContent(n2.Id, "Список дел\n- Экспорт в ProRes для продюсера\n- Забрать диск у оператора", now);

        services.AppCatalog.SetForPreview(new[]
        {
            new AppEntry("Калькулятор", "Microsoft.WindowsCalculator_8wekyb3d8bbwe!App"),
            new AppEntry("Календарь", "microsoft.windowscommunicationsapps_8wekyb3d8bbwe!microsoft.windowslive.calendar"),
            new AppEntry("DaVinci Resolve", "{6D809377-6AF0-444B-8957-A3773F02200E}\\Blackmagic Design\\DaVinci Resolve\\Resolve.exe"),
            new AppEntry("Adobe Premiere Pro 2026", "{6D809377-6AF0-444B-8957-A3773F02200E}\\Adobe\\Adobe Premiere Pro 2026\\Adobe Premiere Pro.exe"),
            new AppEntry("Telegram", "Telegram"),
        });
    }

    private static ClipEntry? SampleImage()
    {
        const int w = 480, h = 270;
        var pixels = new byte[w * h * 4];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            int i = (y * w + x) * 4;
            double t = (double)x / w, v = (double)y / h;
            pixels[i] = (byte)(40 + 60 * v);                     // B
            pixels[i + 1] = (byte)(60 + 110 * t * (1 - v));      // G
            pixels[i + 2] = (byte)(90 + 160 * t);                // R
            pixels[i + 3] = 255;
        }
        var bitmap = BitmapSource.Create(w, h, 96, 96, PixelFormats.Bgra32, null, pixels, w * 4);
        var hash = ClipHash.OfBytes(pixels);
        var name = hash[..32] + ".png";
        Directory.CreateDirectory(AppPaths.ClipboardImagesDir);
        using (var fs = File.Create(Path.Combine(AppPaths.ClipboardImagesDir, name)))
        {
            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));
            encoder.Save(fs);
        }
        return ClipEntry.FromImage(name, w, h, hash, "SnippingTool.exe", DateTimeOffset.Now);
    }

    private static void RenderWindow(Window window, double width, double? height, string file, bool refresh = false, bool transparent = false)
    {
        var content = (FrameworkElement)window.Content;
        var resources = window.Resources;
        window.Content = null;
        var host = new Border { Child = content };
        foreach (var key in resources.Keys) host.Resources[key] = resources[key];
        if (!transparent) host.SetResourceReference(Border.BackgroundProperty, "Grip.Bg");
        host.SetResourceReference(TextElement.FontFamilyProperty, "Grip.Font");
        host.SetResourceReference(TextElement.ForegroundProperty, "Grip.Text");
        TextElement.SetFontSize(host, 13);
        TextOptions.SetTextFormattingMode(host, TextFormattingMode.Ideal);
        if (refresh)
        {
            host.Measure(new Size(width, height ?? double.PositiveInfinity));
            foreach (var section in FindAll<PanelSection>(host)) section.Refresh();
        }
        RenderElement(host, width, height, file, transparent);
        window.Close();
    }

    private static void RenderElement(FrameworkElement element, double width, double? height, string file, bool transparent = false)
    {
        // Items and templates materialize lazily; repeat until the height stops changing.
        var size = new Size(width, height ?? 0);
        for (int pass = 0; pass < 4; pass++)
        {
            element.Measure(new Size(width, height ?? double.PositiveInfinity));
            var next = new Size(width, height ?? Math.Ceiling(element.DesiredSize.Height));
            element.Arrange(new Rect(next));
            element.UpdateLayout();
            if (next == size) break;
            size = next;
        }

        var bitmap = new RenderTargetBitmap((int)(size.Width * Scale), (int)(size.Height * Scale), 96 * Scale, 96 * Scale, PixelFormats.Pbgra32);
        if (!transparent)
        {
            var bg = new DrawingVisual();
            using (var dc = bg.RenderOpen())
                dc.DrawRectangle((Brush)Application.Current.FindResource("Grip.Bg"), null, new Rect(size));
            bitmap.Render(bg);
        }
        bitmap.Render(element);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var fs = File.Create(file);
        encoder.Save(fs);
    }

    private static IEnumerable<T> FindAll<T>(DependencyObject root) where T : DependencyObject
    {
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) yield return match;
            foreach (var nested in FindAll<T>(child)) yield return nested;
        }
    }
}
