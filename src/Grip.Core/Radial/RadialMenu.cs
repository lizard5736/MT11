using Grip.Core.Actions;

namespace Grip.Core.Radial;

public enum RadialItemKind
{
    /// <summary>A Grip or system action from <see cref="ActionIds"/>.</summary>
    Action,
    /// <summary>An executable, a shortcut or a Start menu app id.</summary>
    App,
    File,
    Folder,
    Url,
    /// <summary>A key combination sent to the app in front, e.g. "Ctrl+Shift+Esc".</summary>
    Keys,
    /// <summary>Opens a nested ring.</summary>
    Submenu,
}

public sealed class RadialItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public RadialItemKind Kind { get; set; }
    public string Label { get; set; } = "";
    public string Target { get; set; } = "";
    public string? Arguments { get; set; }
    /// <summary>Icon key from the Grip icon set; empty picks one from the kind.</summary>
    public string? Icon { get; set; }
    public List<RadialItem> Children { get; set; } = new();

    public RadialItem Clone() => new()
    {
        Id = Id, Kind = Kind, Label = Label, Target = Target, Arguments = Arguments, Icon = Icon,
        Children = Children.Select(c => c.Clone()).ToList(),
    };
}

public sealed class RadialProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public List<RadialItem> Items { get; set; } = new();

    public const int MaxItems = 12;
    public const int MinItems = 2;

    /// <summary>The ring a clean install starts with. Labels are localized keys (prefixed with '@').</summary>
    public static RadialProfile CreateDefault() => new()
    {
        Id = "main",
        Name = "@radial.profile.main",
        Items = new List<RadialItem>
        {
            new() { Kind = RadialItemKind.Action, Target = ActionIds.CommandBar, Label = "@radial.item.commandBar", Icon = "WindowConsole" },
            new() { Kind = RadialItemKind.Action, Target = ActionIds.ClipboardHistory, Label = "@radial.item.clipboard", Icon = "Clipboard" },
            new() { Kind = RadialItemKind.Keys, Target = "Win+Shift+S", Label = "@radial.item.snip", Icon = "Screenshot" },
            new() { Kind = RadialItemKind.App, Target = "explorer.exe", Label = "@radial.item.explorer", Icon = "Folder" },
            new()
            {
                Kind = RadialItemKind.Submenu, Label = "@radial.item.media", Icon = "MusicNote",
                Children = new List<RadialItem>
                {
                    new() { Kind = RadialItemKind.Action, Target = ActionIds.MediaPlayPause, Icon = "Play" },
                    new() { Kind = RadialItemKind.Action, Target = ActionIds.MediaNext, Icon = "Next" },
                    new() { Kind = RadialItemKind.Action, Target = ActionIds.VolumeUp, Icon = "Speaker" },
                    new() { Kind = RadialItemKind.Action, Target = ActionIds.VolumeMute, Icon = "SpeakerMute" },
                    new() { Kind = RadialItemKind.Action, Target = ActionIds.VolumeDown, Icon = "SpeakerLow" },
                    new() { Kind = RadialItemKind.Action, Target = ActionIds.MediaPrevious, Icon = "Previous" },
                },
            },
            new() { Kind = RadialItemKind.Keys, Target = "Ctrl+Shift+Esc", Label = "@radial.item.taskManager", Icon = "DataPie" },
            new() { Kind = RadialItemKind.Action, Target = ActionIds.KeepAwakeToggle, Label = "@radial.item.keepAwake", Icon = "WeatherMoon" },
            new() { Kind = RadialItemKind.Action, Target = ActionIds.LockScreen, Label = "@radial.item.lock", Icon = "LockClosed" },
        },
    };
}

/// <summary>Pure geometry for the ring: which slice the pointer is over.</summary>
public static class RadialGeometry
{
    /// <summary>
    /// Returns the slice index under an offset from the ring center, or -1
    /// inside the dead zone. Slice 0 is centered straight up and indices grow
    /// clockwise (screen coordinates, y pointing down).
    /// </summary>
    public static int HitTest(double dx, double dy, int count, double deadZone)
    {
        if (count <= 0) return -1;
        if (dx * dx + dy * dy < deadZone * deadZone) return -1;
        double angle = AngleFromTop(dx, dy);
        double slice = 360.0 / count;
        int index = (int)Math.Floor((angle + slice / 2) / slice) % count;
        return index;
    }

    /// <summary>Clockwise angle in degrees from straight up, in [0, 360).</summary>
    public static double AngleFromTop(double dx, double dy)
    {
        double radians = Math.Atan2(dx, -dy); // 0 at top, clockwise positive
        double degrees = radians * 180.0 / Math.PI;
        if (degrees < 0) degrees += 360.0;
        return degrees;
    }

    /// <summary>Center angle of a slice, clockwise from the top.</summary>
    public static double SliceCenter(int index, int count) => count <= 0 ? 0 : index * 360.0 / count;

    /// <summary>Point on a circle at a clockwise angle from the top (screen coordinates).</summary>
    public static (double X, double Y) PointAt(double centerX, double centerY, double radius, double angleFromTop)
    {
        double radians = angleFromTop * Math.PI / 180.0;
        return (centerX + radius * Math.Sin(radians), centerY - radius * Math.Cos(radians));
    }
}
