using System.Windows;
using System.Windows.Controls;
using Grip.Core.Actions;
using Grip.Core.Input;
using Grip.Core.Localization;
using Grip.Core.Radial;
using Grip.Core.Settings;
using Grip.UI.Controls;
using Grip.UI.Radial;

namespace Grip.UI.Settings.Pages;

/// <summary>Radial menu settings: how to open it, and a profile editor with a live preview of the ring.</summary>
public static class RadialPage
{
    private static Style S(string key) => (Style)Application.Current.FindResource(key);

    public static void Build(PageBuilder b)
    {
        b.Card();
        b.Hotkey(ActionIds.RadialMenu);
        b.Choice("settings.radial.trigger", "settings.radial.trigger.desc",
            Enum.GetValues<RadialMouseTrigger>().Select(t => (t, L.S("settings.radial.trigger." + t))),
            s => s.RadialMenu.MouseTrigger, (s, v) => s.RadialMenu.MouseTrigger = v, icon: "Cursor");
        b.Choice("settings.radial.size", null,
            Enum.GetValues<RadialSize>().Select(t => (t, L.S("settings.radial.size." + t))),
            s => s.RadialMenu.Size, (s, v) => s.RadialMenu.Size = v, icon: "DataPie");
        b.Toggle("settings.radial.release", "settings.radial.release.desc", s => s.RadialMenu.ReleaseToSelect,
            (s, v) => s.RadialMenu.ReleaseToSelect = v, icon: "Keyboard");

        b.Card("settings.radial.items", "settings.radial.items.desc");
        b.Custom(new RadialEditor(), L.S("settings.radial.items"), L.S("settings.radial.items.desc"));
    }

    /// <summary>Profile picker, item list and ring preview side by side.</summary>
    private sealed class RadialEditor : Grid
    {
        private readonly StackPanel _list = new();
        private readonly RadialMenuView _preview = new();
        private readonly StackPanel _profileBar = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
        private RadialItem? _openSubmenu;

        public RadialEditor()
        {
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250) });
            var left = new StackPanel();
            left.Children.Add(_profileBar);
            left.Children.Add(_list);
            Children.Add(left);

            var previewHost = new StackPanel { Margin = new Thickness(16, 0, 0, 0), HorizontalAlignment = HorizontalAlignment.Center };
            previewHost.Children.Add(new TextBlock { Text = L.S("settings.radial.preview"), Style = S("Grip.Text.Caption"), HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 6) });
            _preview.LayoutTransform = new System.Windows.Media.ScaleTransform(0.72, 0.72);
            previewHost.Children.Add(_preview);
            SetColumn(previewHost, 1);
            Children.Add(previewHost);
            Fill();
        }

        private static RadialMenuSettings Settings => App.Services.Settings.Current.RadialMenu;

        private static RadialProfile Profile =>
            Settings.Profiles.FirstOrDefault(p => p.Id == Settings.ActiveProfileId) ?? Settings.Profiles[0];

        private static void Save() => App.Services.Settings.Touch();

        private void Fill()
        {
            BuildProfileBar();
            _list.Children.Clear();
            var items = _openSubmenu?.Children ?? Profile.Items;

            if (_openSubmenu != null)
            {
                var back = new Button { Style = S("Grip.Button.Ghost"), Content = L.S("radial.back") + ": " + RadialMenuView.LabelOf(_openSubmenu), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 0, 0, 8) };
                Ui.SetIcon(back, "ArrowLeft");
                back.Click += (_, _) =>
                {
                    _openSubmenu = null;
                    Fill();
                };
                _list.Children.Add(back);
            }

            for (int i = 0; i < items.Count; i++) _list.Children.Add(ItemRow(items, i));

            var add = new Button { Style = S("Grip.Button"), Content = L.S("settings.radial.item.add"), HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 6, 0, 0), IsEnabled = items.Count < RadialProfile.MaxItems };
            Ui.SetIcon(add, "Add");
            add.Click += (_, _) =>
            {
                items.Add(new RadialItem { Kind = RadialItemKind.Action, Target = ActionIds.CommandBar });
                Save();
                Fill();
            };
            _list.Children.Add(add);
            _preview.SetItems(items, _openSubmenu != null, RadialMenuView.RadiusFor(Settings.Size));
        }

        private void BuildProfileBar()
        {
            _profileBar.Children.Clear();
            var loc = Localizer.Instance;
            var combo = PageBuilder.ComboFor(Settings.Profiles.Select(p => (p.Id, loc.Resolve(p.Name))), Settings.ActiveProfileId, id =>
            {
                App.Services.Settings.Update(s => s.RadialMenu.ActiveProfileId = id);
                _openSubmenu = null;
                Fill();
            }, width: 200);
            _profileBar.Children.Add(combo);

            var add = GeneralPage.IconButton("Add", L.S("settings.radial.profile.add"), true, () =>
            {
                var profile = new RadialProfile
                {
                    Name = L.F("settings.radial.profile.newName", Settings.Profiles.Count + 1),
                    Items = Profile.Items.Select(i => i.Clone()).ToList(),
                };
                App.Services.Settings.Update(s =>
                {
                    s.RadialMenu.Profiles.Add(profile);
                    s.RadialMenu.ActiveProfileId = profile.Id;
                });
                _openSubmenu = null;
                Fill();
            });
            add.Margin = new Thickness(8, 0, 0, 0);
            _profileBar.Children.Add(add);

            var delete = GeneralPage.IconButton("Delete", L.S("settings.radial.profile.delete"), Settings.Profiles.Count > 1, () =>
            {
                App.Services.Settings.Update(s =>
                {
                    s.RadialMenu.Profiles.RemoveAll(p => p.Id == s.RadialMenu.ActiveProfileId);
                    s.RadialMenu.ActiveProfileId = s.RadialMenu.Profiles[0].Id;
                });
                _openSubmenu = null;
                Fill();
            });
            _profileBar.Children.Add(delete);
        }

        private FrameworkElement ItemRow(List<RadialItem> items, int index)
        {
            var item = items[index];
            var card = new Border
            {
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 8, 6, 8),
                Margin = new Thickness(0, 0, 0, 6),
            };
            card.SetResourceReference(Border.BackgroundProperty, "Grip.Control");
            // Row 1: number, type, move/delete. Row 2: what it runs. Row 3: optional label.
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(26) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());
            grid.RowDefinitions.Add(new RowDefinition());

            var number = new TextBlock { Text = (index + 1).ToString(), Style = S("Grip.Text.Caption"), VerticalAlignment = VerticalAlignment.Center };
            grid.Children.Add(number);

            var kind = PageBuilder.ComboFor(Enum.GetValues<RadialItemKind>().Where(k => _openSubmenu == null || k != RadialItemKind.Submenu)
                .Select(k => (k, L.S("radial.kind." + k))), item.Kind, k =>
            {
                item.Kind = k;
                item.Target = k == RadialItemKind.Action ? ActionIds.CommandBar : "";
                item.Label = "";
                Save();
                Fill();
            }, width: 170);
            kind.HorizontalAlignment = HorizontalAlignment.Left;
            Grid.SetColumn(kind, 1);
            grid.Children.Add(kind);

            var target = TargetEditor(item);
            target.Margin = new Thickness(0, 6, 0, 0);
            Grid.SetRow(target, 1);
            Grid.SetColumn(target, 1);
            Grid.SetColumnSpan(target, 2);
            grid.Children.Add(target);

            var tools = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(6, 0, 0, 0) };
            tools.Children.Add(GeneralPage.IconButton("ChevronUp", L.S("common.moveUp"), index > 0, () => Move(items, index, -1)));
            tools.Children.Add(GeneralPage.IconButton("ChevronDown", L.S("common.moveDown"), index < items.Count - 1, () => Move(items, index, +1)));
            tools.Children.Add(GeneralPage.IconButton("Delete", L.S("common.delete"), items.Count > RadialProfile.MinItems, () =>
            {
                items.RemoveAt(index);
                Save();
                Fill();
            }));
            Grid.SetColumn(tools, 2);
            grid.Children.Add(tools);

            var label = new TextBox { Style = S("Grip.TextBox"), Text = item.Label.StartsWith('@') ? "" : item.Label, Margin = new Thickness(0, 6, 0, 0) };
            TextBoxHelper.SetPlaceholder(label, L.S("settings.radial.item.label") + ": " + RadialMenuView.LabelOf(item));
            label.LostKeyboardFocus += (_, _) =>
            {
                item.Label = label.Text.Trim();
                Save();
                _preview.InvalidateVisual();
            };
            Grid.SetRow(label, 2);
            Grid.SetColumn(label, 1);
            Grid.SetColumnSpan(label, 2);
            grid.Children.Add(label);

            card.Child = grid;
            return card;
        }

        private FrameworkElement TargetEditor(RadialItem item)
        {
            switch (item.Kind)
            {
                case RadialItemKind.Action:
                    return PageBuilder.ComboFor(ActionCatalog.All.Where(a => a.Id is not ActionIds.RadialMenu)
                        .Select(a => (a.Id, L.S(ActionCatalog.TitleKey(a.Id)))), item.Target, id =>
                    {
                        item.Target = id;
                        Save();
                        _preview.InvalidateVisual();
                    }, width: 260);

                case RadialItemKind.Keys:
                {
                    var box = new HotkeyBox { Value = Hotkey.Parse(item.Target), HorizontalAlignment = HorizontalAlignment.Left };
                    box.ValueChanged += (_, hotkey) =>
                    {
                        item.Target = hotkey.ToString();
                        Save();
                        _preview.InvalidateVisual();
                    };
                    return box;
                }

                case RadialItemKind.Submenu:
                {
                    var open = new Button { Style = S("Grip.Button"), Content = $"{L.S("settings.radial.item.children")} ({item.Children.Count})", HorizontalAlignment = HorizontalAlignment.Left };
                    Ui.SetIcon(open, "ChevronRight");
                    open.Click += (_, _) =>
                    {
                        if (item.Children.Count < RadialProfile.MinItems)
                        {
                            item.Children.Add(new RadialItem { Kind = RadialItemKind.Action, Target = ActionIds.MediaPlayPause });
                            item.Children.Add(new RadialItem { Kind = RadialItemKind.Action, Target = ActionIds.MediaNext });
                            Save();
                        }
                        _openSubmenu = item;
                        Fill();
                    };
                    return open;
                }

                default:
                {
                    var panel = new DockPanel();
                    var box = new TextBox { Style = S("Grip.TextBox"), Text = item.Target };
                    TextBoxHelper.SetPlaceholder(box, item.Kind == RadialItemKind.Url ? "https://" : L.S("settings.radial.item.target"));
                    box.LostKeyboardFocus += (_, _) =>
                    {
                        item.Target = box.Text.Trim();
                        Save();
                        _preview.InvalidateVisual();
                    };
                    if (item.Kind is RadialItemKind.App or RadialItemKind.File or RadialItemKind.Folder)
                    {
                        var browse = GeneralPage.IconButton("FolderOpen", L.S("common.browse"), true, () =>
                        {
                            var path = Browse(item.Kind);
                            if (path == null) return;
                            box.Text = path;
                            item.Target = path;
                            Save();
                            _preview.InvalidateVisual();
                        });
                        browse.Margin = new Thickness(4, 0, 0, 0);
                        DockPanel.SetDock(browse, Dock.Right);
                        panel.Children.Add(browse);
                    }
                    panel.Children.Add(box);
                    return panel;
                }
            }
        }

        private static string? Browse(RadialItemKind kind)
        {
            if (kind == RadialItemKind.Folder)
            {
                var dialog = new Microsoft.Win32.OpenFolderDialog();
                return dialog.ShowDialog() == true ? dialog.FolderName : null;
            }
            var file = new Microsoft.Win32.OpenFileDialog
            {
                Filter = kind == RadialItemKind.App ? "Apps (*.exe;*.lnk)|*.exe;*.lnk|*.*|*.*" : "*.*|*.*",
            };
            return file.ShowDialog() == true ? file.FileName : null;
        }

        private void Move(List<RadialItem> items, int index, int delta)
        {
            var item = items[index];
            items.RemoveAt(index);
            items.Insert(index + delta, item);
            Save();
            Fill();
        }
    }
}
