using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Grip.Interop;
using Grip.UI.Common;

namespace Grip.UI.CommandBar;

public sealed class RowTemplateSelector : DataTemplateSelector
{
    public DataTemplate? Result { get; set; }
    public DataTemplate? Header { get; set; }

    public override DataTemplate? SelectTemplate(object item, DependencyObject container) =>
        item is CommandRow { IsHeader: true } ? Header : Result;
}

/// <summary>One field for apps, windows, settings, math, units and emoji. Esc or a click away closes it.</summary>
public partial class CommandBarWindow : Window
{
    private readonly CommandBarEngine _engine;
    private List<CommandRow> _rows = new();

    public bool IsPreview { get; init; }

    public CommandBarWindow()
    {
        InitializeComponent();
        _engine = new CommandBarEngine(App.Services);
        Results.ItemContainerStyle = BuildContainerStyle();
        SourceInitialized += (_, _) =>
        {
            if (IsPreview) return;
            WindowStyling.MakeToolWindow(this);
            var theme = App.Services.Theme;
            WindowStyling.ApplyChrome(this, theme.Color("Bg"), theme.Color("Text"), theme.Color("Line"), theme.IsDark);
        };
        Deactivated += (_, _) =>
        {
            if (!IsPreview) CloseSoft();
        };
        PreviewKeyDown += OnKey;
        App.Services.AppCatalog.Updated += (_, _) =>
        {
            if (IsVisible) Search();
        };
        App.Services.AppCatalog.IconLoaded += (_, path) =>
        {
            if (!IsVisible) return;
            foreach (var row in _rows.Where(r => r.IconPath == path)) row.Image = App.Services.AppCatalog.Icon(path);
            Results.Items.Refresh();
        };
    }

    private Style BuildContainerStyle()
    {
        var style = new Style(typeof(ListBoxItem), (Style)FindResource("Grip.ListItem"));
        var trigger = new DataTrigger { Binding = new System.Windows.Data.Binding(nameof(CommandRow.IsHeader)), Value = true };
        trigger.Setters.Add(new Setter(IsHitTestVisibleProperty, false));
        trigger.Setters.Add(new Setter(FocusableProperty, false));
        trigger.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(8, 0, 8, 0)));
        style.Triggers.Add(trigger);
        return style;
    }

    public void Toggle()
    {
        if (IsVisible)
        {
            CloseSoft();
            return;
        }
        _engine.Target = NativeMethods.GetForegroundWindow();
        _engine.TakeSnapshot();
        App.Services.AppCatalog.Refresh();
        QueryBox.Text = "";
        Search();
        PopupMotion.PrepareEnter(Root, RootShift);
        // Position before Show(): otherwise the first frame paints at whatever spot
        // Windows' default placement picks (near the screen's top-left) for an instant.
        WindowPlacement.PlaceOnMonitor(this, Screens.FromPoint(Screens.CursorPosition()), 0.22);
        Show();
        WindowStyling.ForceForeground(this);
        Activate();
        QueryBox.Focus();
        PopupMotion.Enter(Root, RootShift);
    }

    /// <summary>Fades out and hides — for the user changing their mind, not for running a result.</summary>
    private void CloseSoft()
    {
        if (!IsVisible) return;
        PopupMotion.Exit(Root, RootShift, Hide);
    }

    /// <summary>Preview helper: show results for a query without any window handling.</summary>
    public void PreviewQuery(string query)
    {
        QueryBox.Text = query;
        Search();
    }

    private void OnQuery(object sender, TextChangedEventArgs e) => Search();

    private void Search()
    {
        _rows = _engine.Search(QueryBox.Text);
        Results.ItemsSource = _rows;
        bool any = _rows.Count > 0;
        Results.Visibility = any ? Visibility.Visible : Visibility.Collapsed;
        ResultsHost.Visibility = any ? Visibility.Visible : Visibility.Collapsed;
        Footer.Visibility = any ? Visibility.Visible : Visibility.Collapsed;
        LayoutHint.Visibility = _engine.CorrectedQuery != null ? Visibility.Visible : Visibility.Collapsed;
        LayoutHint.Text = _engine.CorrectedQuery != null ? L.F("commandBar.layoutFixed", _engine.CorrectedQuery) : "";
        var first = _rows.FirstOrDefault(r => !r.IsHeader);
        Results.SelectedItem = first;
        if (first != null) Results.ScrollIntoView(first);
    }

    private void OnKey(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                if (QueryBox.Text.Length > 0) QueryBox.Text = "";
                else CloseSoft();
                e.Handled = true;
                break;
            case Key.Down:
                Move(+1);
                e.Handled = true;
                break;
            case Key.Up:
                Move(-1);
                e.Handled = true;
                break;
            case Key.Enter:
                Run(Results.SelectedItem as CommandRow);
                e.Handled = true;
                break;
        }
    }

    private void Move(int delta)
    {
        if (_rows.Count == 0) return;
        int index = Results.SelectedIndex;
        for (int step = 0; step < _rows.Count; step++)
        {
            index = (index + delta + _rows.Count) % _rows.Count;
            if (!_rows[index].IsHeader) break;
        }
        Results.SelectedIndex = index;
        Results.ScrollIntoView(Results.SelectedItem);
    }

    private CommandRow? _pressed;

    private void OnResultMouseDown(object sender, MouseButtonEventArgs e) =>
        _pressed = (e.OriginalSource as FrameworkElement)?.DataContext as CommandRow;

    private void OnResultClick(object sender, MouseButtonEventArgs e)
    {
        if ((e.OriginalSource as FrameworkElement)?.DataContext is CommandRow row && row == _pressed && !row.IsHeader) Run(row);
    }

    private void Run(CommandRow? row)
    {
        if (row?.Run == null) return;
        Hide();
        // Let the window disappear before the action takes focus elsewhere.
        Dispatcher.BeginInvoke(() =>
        {
            try { row.Run(); }
            catch (Exception ex) { Services.Log.Error("Command Bar action failed", ex); }
        }, System.Windows.Threading.DispatcherPriority.Background);
    }
}
