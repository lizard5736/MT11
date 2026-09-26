using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using Grip.Core.Settings;
using Grip.Services;
using Grip.UI.Common;
using Grip.UI.Controls;

namespace Grip.UI.Settings.Pages;

public static class AboutPage
{
    public const string RepositoryUrl = "https://github.com/lizard5736/MT11";

    public static void Build(PageBuilder b)
    {
        var header = new DockPanel { Margin = new Thickness(0, 4, 0, 4) };
        header.Children.Add(new GripMark
        {
            Width = 56, Height = 56,
            TileBrush = (System.Windows.Media.Brush)Application.Current.FindResource("Grip.Control"),
            MarkBrush = (System.Windows.Media.Brush)Application.Current.FindResource("Grip.Accent"),
            DotBrush = (System.Windows.Media.Brush)Application.Current.FindResource("Grip.Rec"),
            PixelImage = Application.Current.TryFindResource("Grip.Mark.PixelImage") as System.Windows.Media.ImageSource,
        });
        var text = new StackPanel { Margin = new Thickness(16, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
        text.Children.Add(new TextBlock { Text = "Grip", Style = (Style)Application.Current.FindResource("Grip.Text.Title") });
        var version = typeof(App).Assembly.GetName().Version;
        text.Children.Add(new TextBlock
        {
            Text = L.F("about.version", version == null ? "0.1" : $"{version.Major}.{version.Minor}.{version.Build}"),
            Style = (Style)Application.Current.FindResource("Grip.Text.Secondary"),
        });
        header.Children.Add(text);
        b.Card();
        b.Custom(header);
        b.Note(L.S("about.desc"));
        b.Button("about.repo", RepositoryUrl, "common.open", () => AppCatalogService.Open(RepositoryUrl), icon: "Open", rowIcon: "Globe");

        b.Card("about.transfer", "about.transfer.desc");
        b.Button("about.export", null, "common.save", Export, icon: "ArrowUpload", rowIcon: "ArrowUpload");
        b.Button("about.import", null, "common.open", Import, icon: "ArrowDownload", rowIcon: "ArrowDownload");

        b.Card();
        b.Button("about.data", AppPaths.DataDir, "common.openFolder", () => AccessPage.OpenFolder(AppPaths.DataDir), icon: "FolderOpen", rowIcon: "Folder");
        b.Button("about.logs", AppPaths.LogsDir, "common.openFolder", () => AccessPage.OpenFolder(AppPaths.LogsDir), icon: "FolderOpen", rowIcon: "DocumentText");

        b.Card("about.notices");
        b.Note(L.S("about.notices.text"));
    }

    private static void Export()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            FileName = $"grip-settings-{DateTime.Now:yyyy-MM-dd}.json",
            Filter = L.S("about.fileFilter"),
        };
        if (dialog.ShowDialog() != true) return;
        try
        {
            File.WriteAllText(dialog.FileName, App.Services.Settings.Export());
            App.Services.Hud.Show(L.S("about.exported"), "ArrowUpload", HudTone.Success, force: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ConfirmDialog.Ask(L.F("about.importFailed", ex.Message));
        }
    }

    private static void Import()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = L.S("about.fileFilter") };
        if (dialog.ShowDialog() != true) return;
        try
        {
            var imported = SettingsStore.Deserialize(File.ReadAllText(dialog.FileName));
            imported.General.OnboardingCompleted = true;
            App.Services.Settings.Replace(imported);
            AutostartService.Set(imported.General.LaunchAtStartup);
            App.Services.Hud.Show(L.S("about.imported"), "ArrowDownload", HudTone.Success, force: true);
            foreach (var window in Application.Current.Windows.OfType<SettingsWindow>()) window.RequestRebuild();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            ConfirmDialog.Ask(L.F("about.importFailed", ex.Message));
        }
    }
}
