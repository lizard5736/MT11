using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Grip.Core.Settings;
using Grip.Services;
using Grip.UI.Common;
using Grip.UI.Controls;
using Grip.UI.Panel;

namespace Grip.UI.Settings.Pages;

public static class GeneralPage
{
    public static void Build(PageBuilder b)
    {
        b.Card();
        b.Choice("general.language", null, new[]
        {
            (AppLanguage.System, L.S("general.language.system")),
            (AppLanguage.Russian, "Русский"),
            (AppLanguage.English, "English"),
        }, s => s.General.Language, (s, v) => s.General.Language = v, icon: "Globe");
        b.Choice("general.theme", null, new[]
        {
            (AppTheme.Dark, L.S("general.theme.dark")),
            (AppTheme.Light, L.S("general.theme.light")),
            (AppTheme.System, L.S("general.theme.system")),
            (AppTheme.Doom, L.S("general.theme.doom")),
        }, s => s.General.Theme, (s, v) => s.General.Theme = v, icon: "DarkTheme");
        b.Toggle("general.startup", "general.startup.desc", s => s.General.LaunchAtStartup, (s, v) =>
        {
            s.General.LaunchAtStartup = v;
            AutostartService.Set(v);
        }, icon: "Flash");
        b.Toggle("general.hud", "general.hud.desc", s => s.General.ShowHud, (s, v) => s.General.ShowHud = v, icon: "Alert");

        b.Card("general.panel");
        b.Choice("general.panel.layout", "general.panel.layout.desc", new[]
        {
            (PanelLayoutMode.Tabs, L.S("panel.layout.tabs")),
            (PanelLayoutMode.List, L.S("panel.layout.list")),
        }, s => s.Panel.Layout, (s, v) => s.Panel.Layout = v, icon: "Tabs");
        b.Toggle("general.panel.compact", "general.panel.compact.desc", s => s.Panel.Compact, (s, v) => s.Panel.Compact = v, icon: "Board");
        b.Custom(SectionsEditor(), L.S("general.panel.sections"), L.S("general.panel.sections.desc"));

        b.Card("general.trayHint.title", "general.trayHint.desc");
        b.Button("general.trayHint.title", null, "general.trayHint.open",
            () => AppCatalogService.Open("ms-settings:taskbar"), icon: "Open", rowIcon: "Navigation");

        b.Card();
        b.Button("general.reset", "general.reset.desc", "common.reset", () =>
        {
            if (!ConfirmDialog.Ask(L.S("general.reset.confirm"), okText: L.S("common.reset"), destructive: true)) return;
            var fresh = new AppSettings { General = { OnboardingCompleted = true } };
            App.Services.Settings.Replace(fresh);
            AutostartService.Set(fresh.General.LaunchAtStartup);
            foreach (var window in Application.Current.Windows.OfType<SettingsWindow>()) window.RequestRebuild();
        }, destructive: true, rowIcon: "ArrowReset");
    }

    /// <summary>Reorder and hide panel sections.</summary>
    private static FrameworkElement SectionsEditor()
    {
        var root = new StackPanel();
        var header = new StackPanel { Margin = new Thickness(0, 2, 0, 10) };
        header.Children.Add(new TextBlock { Text = L.S("general.panel.sections"), Style = (Style)Application.Current.FindResource("Grip.Text.Body") });
        header.Children.Add(new TextBlock { Text = L.S("general.panel.sections.desc"), Style = (Style)Application.Current.FindResource("Grip.Text.Secondary"), Margin = new Thickness(0, 3, 0, 0) });
        root.Children.Add(header);
        var list = new StackPanel();
        root.Children.Add(list);

        void Fill()
        {
            list.Children.Clear();
            var settings = App.Services.Settings.Current;
            var ordered = PanelSectionDef.Ordered(settings);
            for (int i = 0; i < ordered.Count; i++)
            {
                var def = ordered[i];
                int index = i;
                var row = new DockPanel { Margin = new Thickness(0, 2, 0, 2) };
                var visible = new ToggleButton
                {
                    Style = (Style)Application.Current.FindResource("Grip.Toggle"),
                    IsChecked = !settings.Panel.HiddenSections.Contains(def.Id),
                    Margin = new Thickness(10, 0, 0, 0),
                };
                visible.Click += (_, _) => App.Services.Settings.Update(s =>
                {
                    s.Panel.HiddenSections.Remove(def.Id);
                    if (visible.IsChecked != true) s.Panel.HiddenSections.Add(def.Id);
                });
                DockPanel.SetDock(visible, Dock.Right);
                row.Children.Add(visible);

                var down = IconButton("ChevronDown", L.S("common.moveDown"), index < ordered.Count - 1, () => Move(ordered, index, +1, Fill));
                var up = IconButton("ChevronUp", L.S("common.moveUp"), index > 0, () => Move(ordered, index, -1, Fill));
                DockPanel.SetDock(down, Dock.Right);
                DockPanel.SetDock(up, Dock.Right);
                row.Children.Add(down);
                row.Children.Add(up);

                var glyph = new Icon { Kind = def.Icon, Size = 16, Margin = new Thickness(2, 0, 10, 0) };
                glyph.SetResourceReference(Icon.ForegroundProperty, "Grip.TextSecondary");
                row.Children.Add(glyph);
                row.Children.Add(new TextBlock { Text = L.S(def.TitleKey), Style = (Style)Application.Current.FindResource("Grip.Text.Body"), VerticalAlignment = VerticalAlignment.Center });
                list.Children.Add(row);
            }
        }

        Fill();
        return root;
    }

    private static void Move(List<PanelSectionDef> ordered, int index, int delta, Action refill)
    {
        var ids = ordered.Select(d => d.Id).ToList();
        var id = ids[index];
        ids.RemoveAt(index);
        ids.Insert(index + delta, id);
        App.Services.Settings.Update(s => s.Panel.SectionOrder = ids);
        refill();
    }

    public static Button IconButton(string icon, string tooltip, bool enabled, Action onClick)
    {
        var button = new Button
        {
            Style = (Style)Application.Current.FindResource("Grip.Button.Icon"),
            ToolTip = tooltip,
            IsEnabled = enabled,
            Width = 28,
            Height = 28,
        };
        Ui.SetIcon(button, icon);
        button.Click += (_, _) => onClick();
        return button;
    }
}
