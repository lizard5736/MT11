using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Grip.Core.Notes;
using Grip.Interop;
using Grip.UI.Common;

namespace Grip.UI.Notes;

/// <summary>
/// The scratchpad: plain-text notes in tabs, autosaved as you type. Stays
/// open while you work in other apps (like the Shelf) rather than closing
/// the moment it loses focus (like the Clipboard popup) — it's somewhere to
/// keep writing, not a pick-one-and-dismiss list.
/// </summary>
public partial class NotepadWindow : Window
{
    private string? _selectedId;
    private bool _suppressTextChanged;

    /// <summary>Preview mode renders without positioning or DWM chrome calls.</summary>
    public bool IsPreview { get; init; }

    private static NoteStore Notes => App.Services.Notepad.Notes;

    public NotepadWindow()
    {
        InitializeComponent();
        SourceInitialized += (_, _) =>
        {
            if (IsPreview) return;
            WindowStyling.MakeToolWindow(this);
            var theme = App.Services.Theme;
            WindowStyling.ApplyChrome(this, theme.Color("Bg"), theme.Color("Text"), theme.Color("Line"), theme.IsDark);
        };
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) CloseSoft();
        };
        Notes.Changed += (_, _) => RenderTabs();

        _selectedId = Notes.Notes.Count > 0 ? Notes.Notes[0].Id : null;
        RenderTabs();
        LoadEditor();
    }

    public void Toggle()
    {
        if (IsVisible)
        {
            CloseSoft();
            return;
        }
        PopupMotion.PrepareEnter(Frame, FrameShift);
        // Position before Show(): otherwise the first frame paints at whatever spot
        // Windows' default placement picks (near the screen's top-left) for an instant.
        Position();
        Show();
        WindowStyling.ForceForeground(this);
        Activate();
        Editor.Focus();
        PopupMotion.Enter(Frame, FrameShift);
    }

    private void Position()
    {
        var cursor = Screens.CursorPosition();
        WindowPlacement.PlaceOnMonitor(this, Screens.FromPoint(cursor), 0.5);
    }

    /// <summary>Fades out and hides — the notes stay loaded in the background service.</summary>
    private void CloseSoft()
    {
        if (!IsVisible) return;
        FlushEditor();
        App.Services.Notepad.Save();
        PopupMotion.Exit(Frame, FrameShift, Hide);
    }

    // ---------- tabs ----------

    private void RenderTabs()
    {
        Tabs.Children.Clear();
        foreach (var note in Notes.Notes) Tabs.Children.Add(BuildTab(note));
    }

    private FrameworkElement BuildTab(Note note)
    {
        var wrap = new Grid { Margin = new Thickness(0, 0, 4, 6) };
        wrap.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        wrap.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var tab = new ToggleButton
        {
            Style = (Style)FindResource("Grip.Chip"),
            Content = NoteTitle.From(note.Content, L.S("scratchpad.untitled")),
            IsChecked = note.Id == _selectedId,
            Focusable = false,
        };
        tab.Click += (_, _) => SelectNote(note.Id);
        wrap.Children.Add(tab);

        var close = new Button
        {
            Style = (Style)FindResource("Grip.Button.Icon"),
            Width = 20,
            Height = 20,
            Margin = new Thickness(2, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            ToolTip = L.S("common.close"),
            Focusable = false,
        };
        Ui.SetIcon(close, "Dismiss");
        Grid.SetColumn(close, 1);
        close.Click += (_, _) => CloseNote(note.Id);
        wrap.Children.Add(close);

        return wrap;
    }

    private void SelectNote(string id)
    {
        if (id == _selectedId) return;
        FlushEditor();
        _selectedId = id;
        RenderTabs();
        LoadEditor();
    }

    private void OnNewNote(object sender, RoutedEventArgs e)
    {
        FlushEditor();
        var note = Notes.Add();
        _selectedId = note.Id;
        RenderTabs();
        LoadEditor();
    }

    private void CloseNote(string id)
    {
        var note = Notes.Find(id);
        if (note == null) return;
        if (!string.IsNullOrWhiteSpace(note.Content))
        {
            var title = NoteTitle.From(note.Content, L.S("scratchpad.untitled"));
            if (!ConfirmDialog.Ask(L.F("scratchpad.delete.confirm", title), okText: L.S("common.delete"), destructive: true, owner: this))
                return;
        }

        bool wasSelected = id == _selectedId;
        Notes.Remove(id);
        if (Notes.Notes.Count == 0) Notes.Add(); // never show zero tabs
        if (!wasSelected) return;
        _selectedId = Notes.Notes[0].Id;
        RenderTabs();
        LoadEditor();
    }

    // ---------- editor ----------

    private void LoadEditor()
    {
        var note = _selectedId != null ? Notes.Find(_selectedId) : null;
        _suppressTextChanged = true;
        Editor.Text = note?.Content ?? "";
        _suppressTextChanged = false;
        Editor.Focus();
    }

    private void FlushEditor()
    {
        if (_selectedId != null) Notes.SetContent(_selectedId, Editor.Text, DateTimeOffset.UtcNow);
    }

    private void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_suppressTextChanged || _selectedId == null) return;
        Notes.SetContent(_selectedId, Editor.Text, DateTimeOffset.UtcNow);
    }

    // ---------- window chrome ----------

    private void OnClose(object sender, RoutedEventArgs e) => CloseSoft();

    private void OnDragWindow(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement fe && fe.TemplatedParent is ButtonBase) return;
        try { DragMove(); }
        catch (InvalidOperationException) { }
    }
}
