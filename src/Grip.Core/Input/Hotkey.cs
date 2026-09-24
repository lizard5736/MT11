using System.Globalization;
using System.Text;

namespace Grip.Core.Input;

/// <summary>
/// Modifier flags. The numeric values match Win32 MOD_* so they can be passed
/// straight to RegisterHotKey.
/// </summary>
[Flags]
public enum HotkeyModifiers
{
    None = 0,
    Alt = 0x1,
    Ctrl = 0x2,
    Shift = 0x4,
    Win = 0x8,
}

/// <summary>
/// A global shortcut: modifiers plus one virtual-key code. Persisted as a
/// readable string such as "Win+Alt+V" so the settings file stays editable.
/// </summary>
public readonly record struct Hotkey(HotkeyModifiers Modifiers, int Key)
{
    public static Hotkey None => default;

    public bool IsEmpty => Key == 0;

    /// <summary>A shortcut without Ctrl, Alt or Win would fire while typing.</summary>
    public bool IsUsableGlobally =>
        !IsEmpty
        && ((Modifiers & (HotkeyModifiers.Ctrl | HotkeyModifiers.Alt | HotkeyModifiers.Win)) != 0
            || VirtualKeys.IsFunctionKey(Key));

    public override string ToString() => Format(this);

    /// <summary>Parts in display order, e.g. ["Win", "Alt", "V"].</summary>
    public IReadOnlyList<string> Parts
    {
        get
        {
            if (IsEmpty) return Array.Empty<string>();
            var parts = new List<string>(5);
            if (Modifiers.HasFlag(HotkeyModifiers.Win)) parts.Add("Win");
            if (Modifiers.HasFlag(HotkeyModifiers.Ctrl)) parts.Add("Ctrl");
            if (Modifiers.HasFlag(HotkeyModifiers.Alt)) parts.Add("Alt");
            if (Modifiers.HasFlag(HotkeyModifiers.Shift)) parts.Add("Shift");
            parts.Add(VirtualKeys.GetName(Key));
            return parts;
        }
    }

    public static string Format(Hotkey hotkey) => hotkey.IsEmpty ? string.Empty : string.Join("+", hotkey.Parts);

    public static Hotkey Parse(string? text) => TryParse(text, out var hotkey) ? hotkey : None;

    public static bool TryParse(string? text, out Hotkey hotkey)
    {
        hotkey = None;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var modifiers = HotkeyModifiers.None;
        int key = 0;
        foreach (var raw in text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            switch (raw.ToLowerInvariant())
            {
                case "win": case "windows": case "super": case "meta":
                    modifiers |= HotkeyModifiers.Win; continue;
                case "ctrl": case "control": case "ctl":
                    modifiers |= HotkeyModifiers.Ctrl; continue;
                case "alt": case "option":
                    modifiers |= HotkeyModifiers.Alt; continue;
                case "shift":
                    modifiers |= HotkeyModifiers.Shift; continue;
            }
            if (key != 0) return false; // two non-modifier keys
            if (!VirtualKeys.TryParse(raw, out key)) return false;
        }
        if (key == 0) return false;
        hotkey = new Hotkey(modifiers, key);
        return true;
    }
}

/// <summary>Names for the virtual-key codes a shortcut can use (layout independent).</summary>
public static class VirtualKeys
{
    public const int Back = 0x08, Tab = 0x09, Enter = 0x0D, Pause = 0x13, CapsLock = 0x14, Escape = 0x1B,
        Space = 0x20, PageUp = 0x21, PageDown = 0x22, End = 0x23, Home = 0x24, Left = 0x25, Up = 0x26,
        Right = 0x27, Down = 0x28, PrintScreen = 0x2C, Insert = 0x2D, Delete = 0x2E,
        F1 = 0x70, F24 = 0x87;

    private static readonly Dictionary<int, string> Names = BuildNames();
    private static readonly Dictionary<string, int> Codes = BuildCodes();

    private static Dictionary<int, string> BuildNames()
    {
        var map = new Dictionary<int, string>
        {
            [Back] = "Backspace", [Tab] = "Tab", [Enter] = "Enter", [Pause] = "Pause", [CapsLock] = "CapsLock",
            [Escape] = "Esc", [Space] = "Space", [PageUp] = "PageUp", [PageDown] = "PageDown", [End] = "End",
            [Home] = "Home", [Left] = "Left", [Up] = "Up", [Right] = "Right", [Down] = "Down",
            [PrintScreen] = "PrintScreen", [Insert] = "Insert", [Delete] = "Delete",
            [0x6A] = "Num*", [0x6B] = "Num+", [0x6D] = "Num-", [0x6E] = "Num.", [0x6F] = "Num/",
            [0xBA] = ";", [0xBB] = "=", [0xBC] = ",", [0xBD] = "-", [0xBE] = ".", [0xBF] = "/",
            [0xC0] = "`", [0xDB] = "[", [0xDC] = "\\", [0xDD] = "]", [0xDE] = "'",
            [0xAD] = "VolumeMute", [0xAE] = "VolumeDown", [0xAF] = "VolumeUp",
            [0xB0] = "MediaNext", [0xB1] = "MediaPrev", [0xB2] = "MediaStop", [0xB3] = "MediaPlay",
        };
        for (int c = 'A'; c <= 'Z'; c++) map[c] = ((char)c).ToString();
        for (int c = '0'; c <= '9'; c++) map[c] = ((char)c).ToString();
        for (int i = 0; i < 10; i++) map[0x60 + i] = "Num" + i.ToString(CultureInfo.InvariantCulture);
        for (int i = 0; i < 24; i++) map[F1 + i] = "F" + (i + 1).ToString(CultureInfo.InvariantCulture);
        return map;
    }

    private static Dictionary<string, int> BuildCodes()
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var (code, name) in Names) map[name] = code;
        map["Return"] = Enter;
        map["Escape"] = Escape;
        map["Del"] = Delete;
        map["Ins"] = Insert;
        map["PgUp"] = PageUp;
        map["PgDn"] = PageDown;
        map["PrtSc"] = PrintScreen;
        map["Backspace"] = Back;
        map["Plus"] = 0xBB;
        map["Minus"] = 0xBD;
        return map;
    }

    public static string GetName(int code) =>
        Names.TryGetValue(code, out var name) ? name : "0x" + code.ToString("X2", CultureInfo.InvariantCulture);

    public static bool TryParse(string text, out int code)
    {
        if (Codes.TryGetValue(text, out code)) return true;
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
            && int.TryParse(text.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code)
            && code is > 0 and < 256)
            return true;
        code = 0;
        return false;
    }

    public static bool IsFunctionKey(int code) => code is >= F1 and <= F24;

    /// <summary>Keys that can never be the main key of a shortcut.</summary>
    public static bool IsModifierKey(int code) =>
        code is 0x10 or 0x11 or 0x12 or 0x5B or 0x5C or 0xA0 or 0xA1 or 0xA2 or 0xA3 or 0xA4 or 0xA5;

    public static string Describe(Hotkey hotkey)
    {
        if (hotkey.IsEmpty) return string.Empty;
        var sb = new StringBuilder();
        foreach (var part in hotkey.Parts)
        {
            if (sb.Length > 0) sb.Append(" + ");
            sb.Append(part);
        }
        return sb.ToString();
    }
}
