using System.Windows.Interop;
using Grip.Core.Input;
using static Grip.Interop.NativeMethods;

namespace Grip.Services;

/// <summary>A hidden message-only window for Win32 notifications.</summary>
public sealed class MessageWindow : IDisposable
{
    private readonly HwndSource _source;

    /// <summary>Return true to mark the message handled.</summary>
    public event Func<int, IntPtr, IntPtr, bool>? Message;

    public IntPtr Handle => _source.Handle;

    public MessageWindow(string name)
    {
        var parameters = new HwndSourceParameters(name)
        {
            ParentWindow = HWND_MESSAGE,
            WindowStyle = 0,
            Width = 0,
            Height = 0,
        };
        _source = new HwndSource(parameters);
        _source.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        var listeners = Message;
        if (listeners == null) return IntPtr.Zero;
        foreach (Func<int, IntPtr, IntPtr, bool> listener in listeners.GetInvocationList())
        {
            if (listener(msg, wParam, lParam))
            {
                handled = true;
                break;
            }
        }
        return IntPtr.Zero;
    }

    public void Dispose() => _source.Dispose();
}

public enum HotkeyState
{
    /// <summary>No shortcut assigned.</summary>
    None,
    Active,
    /// <summary>Windows refused it: another app (or Windows itself) owns it.</summary>
    Conflict,
    /// <summary>Two Grip actions asked for the same shortcut; the first one won.</summary>
    Duplicate,
    /// <summary>The action's feature is uninstalled, so nothing is registered.</summary>
    FeatureOff,
}

/// <summary>
/// Registers global shortcuts with RegisterHotKey. That API costs nothing at
/// rest: Windows only wakes Grip when one of its shortcuts is pressed.
/// </summary>
public sealed class HotkeyService : IDisposable
{
    private readonly MessageWindow _window;
    private readonly Dictionary<int, string> _actionsById = new();
    private readonly Dictionary<string, HotkeyState> _states = new();
    private readonly Dictionary<string, string> _duplicateOf = new();
    private Dictionary<string, Hotkey> _desired = new();
    private HashSet<string> _disabled = new();
    private int _suspendCount;

    public event EventHandler<string>? Pressed;
    public event EventHandler? StatesChanged;

    public HotkeyService(MessageWindow window)
    {
        _window = window;
        _window.Message += OnMessage;
    }

    public HotkeyState StateOf(string actionId) => _states.TryGetValue(actionId, out var s) ? s : HotkeyState.None;

    /// <summary>The action that owns the shortcut this one duplicates.</summary>
    public string? DuplicateOwner(string actionId) => _duplicateOf.TryGetValue(actionId, out var owner) ? owner : null;

    public IReadOnlyList<string> Conflicts => _states.Where(s => s.Value == HotkeyState.Conflict).Select(s => s.Key).ToList();

    /// <summary>Registers every shortcut, skipping actions whose feature is not installed.</summary>
    public void Apply(IReadOnlyDictionary<string, Hotkey> hotkeys, IEnumerable<string> disabledActions)
    {
        _desired = hotkeys.ToDictionary(p => p.Key, p => p.Value);
        _disabled = disabledActions.ToHashSet();
        if (_suspendCount == 0) RegisterAll();
        else ComputeStatesWithoutRegistering();
    }

    /// <summary>Temporarily releases every shortcut (while the user records a new one).</summary>
    public void Suspend()
    {
        if (_suspendCount++ == 0) UnregisterAll();
    }

    public void Resume()
    {
        if (_suspendCount == 0) return;
        if (--_suspendCount == 0) RegisterAll();
    }

    private void RegisterAll()
    {
        UnregisterAll();
        _states.Clear();
        _duplicateOf.Clear();
        var taken = new Dictionary<Hotkey, string>();
        int id = 0xB000;
        foreach (var (action, hotkey) in _desired)
        {
            if (hotkey.IsEmpty) { _states[action] = HotkeyState.None; continue; }
            if (_disabled.Contains(action)) { _states[action] = HotkeyState.FeatureOff; continue; }
            if (taken.TryGetValue(hotkey, out var owner))
            {
                _states[action] = HotkeyState.Duplicate;
                _duplicateOf[action] = owner;
                continue;
            }
            bool ok = RegisterHotKey(_window.Handle, id, (uint)hotkey.Modifiers | MOD_NOREPEAT, (uint)hotkey.Key);
            if (ok)
            {
                _actionsById[id] = action;
                _states[action] = HotkeyState.Active;
                taken[hotkey] = action;
                id++;
            }
            else
            {
                _states[action] = HotkeyState.Conflict;
                Log.Warn($"Shortcut {hotkey} for {action} is taken by another app");
            }
        }
        StatesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ComputeStatesWithoutRegistering()
    {
        foreach (var (action, hotkey) in _desired)
            _states[action] = hotkey.IsEmpty ? HotkeyState.None : _disabled.Contains(action) ? HotkeyState.FeatureOff : HotkeyState.Active;
        StatesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UnregisterAll()
    {
        foreach (var id in _actionsById.Keys) UnregisterHotKey(_window.Handle, id);
        _actionsById.Clear();
    }

    private bool OnMessage(int msg, IntPtr wParam, IntPtr lParam)
    {
        if (msg != WM_HOTKEY) return false;
        if (_actionsById.TryGetValue(wParam.ToInt32(), out var action))
            Pressed?.Invoke(this, action);
        return true;
    }

    public void Dispose()
    {
        UnregisterAll();
        _window.Message -= OnMessage;
    }
}
