using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Grip.Core.Actions;
using Grip.Core.Energy;
using Grip.Core.Localization;
using Grip.Core.Settings;
using Grip.Services;
using Grip.UI.Controls;

namespace Grip.UI.Settings.Pages;

public static class ToolPages
{
    private static Style S(string key) => (Style)Application.Current.FindResource(key);

    public static void BuildCommandBar(PageBuilder b)
    {
        b.Card();
        b.Hotkey(ActionIds.CommandBar);

        b.Card("settings.cb.sources");
        b.Toggle("settings.cb.apps", null, s => s.CommandBar.SearchApps, (s, v) => s.CommandBar.SearchApps = v, icon: "Apps");
        b.Toggle("settings.cb.windows", null, s => s.CommandBar.SearchWindows, (s, v) => s.CommandBar.SearchWindows = v, icon: "WindowMultiple");
        b.Toggle("settings.cb.settings", null, s => s.CommandBar.SearchSettings, (s, v) => s.CommandBar.SearchSettings = v, icon: "Settings");
        b.Toggle("settings.cb.clipboard", null, s => s.CommandBar.SearchClipboard, (s, v) => s.CommandBar.SearchClipboard = v, icon: "Clipboard");
        b.Toggle("settings.cb.calc", null, s => s.CommandBar.Calculator, (s, v) => s.CommandBar.Calculator = v, icon: "Calculator");
        b.Toggle("settings.cb.units", null, s => s.CommandBar.Units, (s, v) => s.CommandBar.Units = v, icon: "Ruler");
        b.Toggle("settings.cb.emoji", null, s => s.CommandBar.Emoji, (s, v) => s.CommandBar.Emoji = v, icon: "Emoji");
        b.Toggle("settings.cb.web", null, s => s.CommandBar.WebSearch, (s, v) => s.CommandBar.WebSearch = v, icon: "Globe");
        b.Choice("settings.cb.engine", null, new[]
        {
            (WebSearchEngine.Google, "Google"),
            (WebSearchEngine.Yandex, "Яндекс"),
            (WebSearchEngine.DuckDuckGo, "DuckDuckGo"),
            (WebSearchEngine.Bing, "Bing"),
        }, s => s.CommandBar.SearchEngine, (s, v) => s.CommandBar.SearchEngine = v, icon: "Search");
        b.Toggle("settings.cb.layout", "settings.cb.layout.desc", s => s.CommandBar.FixKeyboardLayout, (s, v) => s.CommandBar.FixKeyboardLayout = v, icon: "Keyboard");

        b.Card("settings.cb.scripts", "settings.cb.scripts.desc");
        b.Custom(ScriptsEditor(), L.S("settings.cb.scripts"), L.S("settings.cb.scripts.desc"));
    }

    private static FrameworkElement ScriptsEditor()
    {
        var root = new StackPanel();
        var list = new StackPanel();
        root.Children.Add(list);

        void Fill()
        {
            list.Children.Clear();
            var scripts = App.Services.Settings.Current.CommandBar.Scripts;
            for (int i = 0; i < scripts.Count; i++)
            {
                var script = scripts[i];
                int index = i;
                var grid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(140) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                TextBox Field(string value, string placeholder, int column, Action<ScriptEntry, string> apply)
                {
                    var box = new TextBox { Style = S("Grip.TextBox"), Text = value, Margin = new Thickness(0, 0, 8, 0) };
                    TextBoxHelper.SetPlaceholder(box, placeholder);
                    box.LostKeyboardFocus += (_, _) => App.Services.Settings.Update(s =>
                    {
                        if (index < s.CommandBar.Scripts.Count) apply(s.CommandBar.Scripts[index], box.Text.Trim());
                    });
                    Grid.SetColumn(box, column);
                    grid.Children.Add(box);
                    return box;
                }

                Field(script.Name, L.S("common.name"), 0, (s, v) => s.Name = v);
                Field(script.Command, L.S("settings.cb.script.command"), 1, (s, v) => s.Command = v);
                Field(script.Arguments, L.S("settings.cb.script.args"), 2, (s, v) => s.Arguments = v);
                var remove = GeneralPage.IconButton("Delete", L.S("common.delete"), true, () =>
                {
                    App.Services.Settings.Update(s => s.CommandBar.Scripts.RemoveAt(index));
                    Fill();
                });
                Grid.SetColumn(remove, 3);
                grid.Children.Add(remove);
                list.Children.Add(grid);
            }
        }

        var add = new Button { Style = S("Grip.Button"), Content = L.S("settings.cb.script.add"), HorizontalAlignment = HorizontalAlignment.Left };
        Ui.SetIcon(add, "Add");
        add.Click += (_, _) =>
        {
            App.Services.Settings.Update(s => s.CommandBar.Scripts.Add(new ScriptEntry { Name = "", Command = "" }));
            Fill();
        };
        Fill();
        root.Children.Add(add);
        return root;
    }

    public static void BuildToggles(PageBuilder b)
    {
        b.Card();
        var list = new StackPanel();
        void Fill()
        {
            list.Children.Clear();
            var saved = App.Services.Settings.Current.QuickToggles.Visible;
            var visible = App.Services.Toggles.Visible(saved).Select(t => t.Id).ToList();
            var order = visible.Concat(QuickToggleService.All.Select(t => t.Id).Where(id => !visible.Contains(id))).ToList();
            for (int i = 0; i < order.Count; i++)
            {
                var info = QuickToggleService.All.First(t => t.Id == order[i]);
                int index = i;
                var row = new DockPanel { Margin = new Thickness(0, 3, 0, 3) };
                var shown = new ToggleButton { Style = S("Grip.Toggle"), IsChecked = visible.Contains(info.Id), Margin = new Thickness(10, 0, 0, 0) };
                shown.Click += (_, _) =>
                {
                    var next = order.Where(id => id == info.Id ? shown.IsChecked == true : visible.Contains(id)).ToList();
                    App.Services.Settings.Update(s => s.QuickToggles.Visible = next);
                    Fill();
                };
                DockPanel.SetDock(shown, Dock.Right);
                row.Children.Add(shown);
                var down = GeneralPage.IconButton("ChevronDown", L.S("common.moveDown"), index < order.Count - 1, () => Move(order, visible, index, +1, Fill));
                var up = GeneralPage.IconButton("ChevronUp", L.S("common.moveUp"), index > 0, () => Move(order, visible, index, -1, Fill));
                DockPanel.SetDock(down, Dock.Right);
                DockPanel.SetDock(up, Dock.Right);
                row.Children.Add(down);
                row.Children.Add(up);
                var glyph = new Icon { Kind = info.Icon, Size = 18, Margin = new Thickness(2, 0, 12, 0) };
                glyph.SetResourceReference(Icon.ForegroundProperty, "Grip.Accent");
                row.Children.Add(glyph);
                row.Children.Add(new TextBlock { Text = L.S(info.TitleKey), Style = S("Grip.Text.Body"), VerticalAlignment = VerticalAlignment.Center });
                list.Children.Add(row);
            }
        }
        Fill();
        b.Custom(list, L.S("settings.page.toggles"), L.S("settings.page.toggles.desc"));
    }

    private static void Move(List<string> order, List<string> visible, int index, int delta, Action refill)
    {
        var ids = order.ToList();
        var id = ids[index];
        ids.RemoveAt(index);
        ids.Insert(index + delta, id);
        App.Services.Settings.Update(s => s.QuickToggles.Visible = ids.Where(visible.Contains).ToList());
        refill();
    }

    public static void BuildKeepAwake(PageBuilder b)
    {
        var loc = Localizer.Instance;
        b.Card();
        b.Hotkey(ActionIds.KeepAwakeToggle);
        b.Choice("settings.keepAwake.default", "settings.keepAwake.default.desc",
            App.Services.Settings.Current.KeepAwake.Presets.Select(m => (m, KeepAwakeText.PresetLabel(m, loc))),
            s => s.KeepAwake.DefaultMinutes, (s, v) => s.KeepAwake.DefaultMinutes = v, icon: "Timer");
        b.Toggle("keepAwake.display", "keepAwake.display.desc", s => s.KeepAwake.KeepDisplayOn, (s, v) => s.KeepAwake.KeepDisplayOn = v, icon: "Desktop");
        b.Toggle("settings.keepAwake.tray", "settings.keepAwake.tray.desc", s => s.KeepAwake.ShowInTrayIcon, (s, v) => s.KeepAwake.ShowInTrayIcon = v, icon: "CircleFilled");

        b.Card("keepAwake.lid");
        if (KeepAwakeService.HasLid)
            b.Toggle("keepAwake.lid", "keepAwake.lid.desc", s => s.KeepAwake.LidClosed, (s, v) => s.KeepAwake.LidClosed = v, icon: "WeatherMoon");
        else
            b.Note(L.S("settings.keepAwake.lid.unsupported"));
    }
}
