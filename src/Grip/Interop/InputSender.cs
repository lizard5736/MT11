using Grip.Core.Input;
using static Grip.Interop.NativeMethods;

namespace Grip.Interop;

/// <summary>Synthesizes key presses with SendInput.</summary>
internal static class InputSender
{
    public const ushort VK_SHIFT = 0x10, VK_CONTROL = 0x11, VK_MENU = 0x12, VK_LWIN = 0x5B, VK_RWIN = 0x5C,
        VK_LSHIFT = 0xA0, VK_RSHIFT = 0xA1, VK_LCONTROL = 0xA2, VK_RCONTROL = 0xA3, VK_LMENU = 0xA4, VK_RMENU = 0xA5,
        VK_V = 0x56, VK_MASK = 0xE8;

    public const ushort VK_VOLUME_MUTE = 0xAD, VK_VOLUME_DOWN = 0xAE, VK_VOLUME_UP = 0xAF,
        VK_MEDIA_NEXT_TRACK = 0xB0, VK_MEDIA_PREV_TRACK = 0xB1, VK_MEDIA_PLAY_PAUSE = 0xB3;

    private static readonly ushort[] HeldModifiers =
        { VK_LSHIFT, VK_RSHIFT, VK_LCONTROL, VK_RCONTROL, VK_LMENU, VK_RMENU, VK_LWIN, VK_RWIN };

    /// <summary>
    /// Lifts modifiers the user is still physically holding (for example the
    /// Alt and Shift of the shortcut that triggered us), so they don't mix
    /// into the keys we send. A neutral key goes first so a released Alt or
    /// Win doesn't open a menu bar or the Start menu.
    /// </summary>
    public static void ReleaseHeldModifiers()
    {
        var inputs = new List<INPUT>();
        bool altOrWin = IsDown(VK_LMENU) || IsDown(VK_RMENU) || IsDown(VK_LWIN) || IsDown(VK_RWIN);
        if (altOrWin)
        {
            inputs.Add(Key(VK_MASK, false));
            inputs.Add(Key(VK_MASK, true));
        }
        foreach (var vk in HeldModifiers)
            if (IsDown(vk)) inputs.Add(Key(vk, true));
        if (inputs.Count > 0) Send(inputs);
    }

    public static void SendCtrlV()
    {
        ReleaseHeldModifiers();
        Send(new List<INPUT>
        {
            Key(VK_CONTROL, false),
            Key(VK_V, false),
            Key(VK_V, true),
            Key(VK_CONTROL, true),
        });
    }

    /// <summary>Presses a whole shortcut such as Ctrl+Shift+Esc.</summary>
    public static void SendHotkey(Hotkey hotkey)
    {
        if (hotkey.IsEmpty) return;
        ReleaseHeldModifiers();
        var down = new List<INPUT>();
        var up = new List<INPUT>();
        void Mod(HotkeyModifiers flag, ushort vk)
        {
            if (!hotkey.Modifiers.HasFlag(flag)) return;
            down.Add(Key(vk, false));
            up.Insert(0, Key(vk, true));
        }
        Mod(HotkeyModifiers.Win, VK_LWIN);
        Mod(HotkeyModifiers.Ctrl, VK_CONTROL);
        Mod(HotkeyModifiers.Alt, VK_MENU);
        Mod(HotkeyModifiers.Shift, VK_SHIFT);
        down.Add(Key((ushort)hotkey.Key, false));
        up.Insert(0, Key((ushort)hotkey.Key, true));
        down.AddRange(up);
        Send(down);
    }

    public static void Tap(ushort vk)
    {
        Send(new List<INPUT> { Key(vk, false), Key(vk, true) });
    }

    private static bool IsDown(ushort vk) => (GetAsyncKeyState(vk) & 0x8000) != 0;

    private static INPUT Key(ushort vk, bool up)
    {
        uint flags = up ? KEYEVENTF_KEYUP : 0;
        if (IsExtended(vk)) flags |= KEYEVENTF_EXTENDEDKEY;
        return new INPUT
        {
            type = INPUT_KEYBOARD,
            U = new InputUnion
            {
                ki = new KEYBDINPUT
                {
                    wVk = vk,
                    wScan = (ushort)MapVirtualKey(vk, 0),
                    dwFlags = flags,
                    dwExtraInfo = GripInputTag,
                },
            },
        };
    }

    /// <summary>Marks our own synthetic input so Grip's hooks can ignore it.</summary>
    public static readonly IntPtr GripInputTag = new(0x47524950); // "GRIP"

    private static bool IsExtended(ushort vk) => vk is VK_LWIN or VK_RWIN or VK_RCONTROL or VK_RMENU
        or >= 0x21 and <= 0x28 or 0x2C or 0x2D or 0x2E or 0x5D or 0x6F or 0x90 or >= 0xA6 and <= 0xB7;

    private static void Send(List<INPUT> inputs)
    {
        if (inputs.Count == 0) return;
        SendInput((uint)inputs.Count, inputs.ToArray(), INPUT.Size);
    }
}
