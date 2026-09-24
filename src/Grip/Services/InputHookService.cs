using System.Runtime.InteropServices;
using System.Windows;
using Grip.Interop;
using static Grip.Interop.NativeMethods;

namespace Grip.Services;

public enum MouseButtonKind { Left, Right, Middle, XButton1, XButton2 }

public readonly record struct MouseEventInfo(int Message, int X, int Y, MouseButtonKind? Button, bool IsDown, bool IsUp, long TimeMs);

/// <summary>
/// A low-level mouse hook on its own thread, installed only while a feature
/// needs it (shake-to-open shelf, radial menu on a mouse button). Running it
/// off the UI thread keeps the pointer smooth even when Grip is busy.
/// Handlers run on the hook thread and must be quick; return true to swallow
/// the event.
/// </summary>
public sealed class InputHookService : IDisposable
{
    private readonly object _gate = new();
    private readonly List<Func<MouseEventInfo, bool>> _handlers = new();
    private Thread? _thread;
    private uint _threadId;
    private IntPtr _hook;
    private LowLevelProc? _proc;

    public bool IsMouseHooked => _hook != IntPtr.Zero;

    public IDisposable Subscribe(Func<MouseEventInfo, bool> handler)
    {
        lock (_gate)
        {
            _handlers.Add(handler);
            if (_thread == null) StartThread();
        }
        return new Subscription(() =>
        {
            lock (_gate)
            {
                _handlers.Remove(handler);
                if (_handlers.Count == 0) StopThread();
            }
        });
    }

    private void StartThread()
    {
        var ready = new ManualResetEventSlim();
        _thread = new Thread(() =>
        {
            _threadId = GetCurrentThreadId();
            _proc = HookProc;
            _hook = SetWindowsHookEx(WH_MOUSE_LL, _proc, GetModuleHandle(null), 0);
            if (_hook == IntPtr.Zero) Log.Warn("Mouse hook failed: " + Marshal.GetLastWin32Error());
            else Log.Info("Mouse hook on");
            ready.Set();
            while (GetMessage(out var msg, IntPtr.Zero, 0, 0) > 0)
            {
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);
            }
            if (_hook != IntPtr.Zero) UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
            Log.Info("Mouse hook off");
        })
        {
            IsBackground = true,
            Name = "Grip mouse hook",
            Priority = ThreadPriority.AboveNormal,
        };
        _thread.Start();
        ready.Wait(2000);
    }

    private void StopThread()
    {
        if (_thread == null) return;
        PostThreadMessage(_threadId, WM_QUIT, IntPtr.Zero, IntPtr.Zero);
        _thread.Join(1000);
        _thread = null;
    }

    private IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var data = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            bool injected = (data.flags & LLMHF_INJECTED) != 0 && data.dwExtraInfo == InputSender.GripInputTag;
            if (!injected)
            {
                int message = wParam.ToInt32();
                MouseButtonKind? button = message switch
                {
                    WM_LBUTTONDOWN or WM_LBUTTONUP => MouseButtonKind.Left,
                    WM_RBUTTONDOWN or WM_RBUTTONUP => MouseButtonKind.Right,
                    WM_MBUTTONDOWN or WM_MBUTTONUP => MouseButtonKind.Middle,
                    WM_XBUTTONDOWN or WM_XBUTTONUP => ((data.mouseData >> 16) & 0xFFFF) == 1 ? MouseButtonKind.XButton1 : MouseButtonKind.XButton2,
                    _ => null,
                };
                bool down = message is WM_LBUTTONDOWN or WM_RBUTTONDOWN or WM_MBUTTONDOWN or WM_XBUTTONDOWN;
                bool up = message is WM_LBUTTONUP or WM_RBUTTONUP or WM_MBUTTONUP or WM_XBUTTONUP;
                var info = new MouseEventInfo(message, data.pt.X, data.pt.Y, button, down, up, Environment.TickCount64);
                Func<MouseEventInfo, bool>[] handlers;
                lock (_gate) handlers = _handlers.ToArray();
                bool swallow = false;
                foreach (var handler in handlers)
                {
                    try { swallow |= handler(info); }
                    catch (Exception ex) { Log.Error("Mouse hook handler failed", ex); }
                }
                if (swallow) return new IntPtr(1);
            }
        }
        return CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _handlers.Clear();
            StopThread();
        }
    }

    private sealed class Subscription : IDisposable
    {
        private Action? _dispose;
        public Subscription(Action dispose) => _dispose = dispose;
        public void Dispose()
        {
            _dispose?.Invoke();
            _dispose = null;
        }
    }

    /// <summary>Marshals work from the hook thread to the UI thread without blocking the hook.</summary>
    public static void OnUi(Action action) => Application.Current?.Dispatcher.BeginInvoke(action);
}
