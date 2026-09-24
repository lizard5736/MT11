namespace Grip.Core.Windows;

/// <summary>A place to snap the focused window to, inside its monitor's work area.</summary>
public enum WindowZone
{
    LeftHalf,
    RightHalf,
    TopHalf,
    BottomHalf,
    TopLeftQuarter,
    TopRightQuarter,
    BottomLeftQuarter,
    BottomRightQuarter,
    Maximize,
}

/// <summary>A target rectangle, in whatever unit the work area was given (Grip always uses physical pixels).</summary>
public readonly record struct ZoneRect(int X, int Y, int Width, int Height);

public static class WindowZones
{
    /// <summary>
    /// Turns a zone into a rectangle within a work area. Uses subtraction rather than a
    /// second division for the far half/quarter, so the two sides always sum to exactly
    /// the full width or height — no 1px gap or overlap down the middle on an odd size.
    /// </summary>
    public static ZoneRect Compute(WindowZone zone, int areaX, int areaY, int areaWidth, int areaHeight)
    {
        int halfW = areaWidth / 2;
        int halfH = areaHeight / 2;
        return zone switch
        {
            WindowZone.LeftHalf => new ZoneRect(areaX, areaY, halfW, areaHeight),
            WindowZone.RightHalf => new ZoneRect(areaX + halfW, areaY, areaWidth - halfW, areaHeight),
            WindowZone.TopHalf => new ZoneRect(areaX, areaY, areaWidth, halfH),
            WindowZone.BottomHalf => new ZoneRect(areaX, areaY + halfH, areaWidth, areaHeight - halfH),
            WindowZone.TopLeftQuarter => new ZoneRect(areaX, areaY, halfW, halfH),
            WindowZone.TopRightQuarter => new ZoneRect(areaX + halfW, areaY, areaWidth - halfW, halfH),
            WindowZone.BottomLeftQuarter => new ZoneRect(areaX, areaY + halfH, halfW, areaHeight - halfH),
            WindowZone.BottomRightQuarter => new ZoneRect(areaX + halfW, areaY + halfH, areaWidth - halfW, areaHeight - halfH),
            WindowZone.Maximize => new ZoneRect(areaX, areaY, areaWidth, areaHeight),
            _ => new ZoneRect(areaX, areaY, areaWidth, areaHeight),
        };
    }
}
