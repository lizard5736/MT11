using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace Grip.UI.Controls;

/// <summary>
/// The classic DOOM (1993) title-screen fire: each pixel is a "heat" value that cools by a
/// random amount and drifts up and sideways into the row above, seeded by a solid line of
/// max heat along the bottom. A small logical buffer stretched with nearest-neighbor scaling
/// gives the chunky, period-correct pixelation on its own — no sprite frames to draw by hand.
///
/// Plain WriteableBitmap.WritePixels, not Lock()/BackBuffer: nothing in this app uses `unsafe`,
/// and at 80x30 pixels/~14fps the managed-array overhead is irrelevant. Also, unlike a pixel
/// Effect/shader, it behaves identically under software rendering — this project's own Wine
/// preview pipeline explicitly disables hardware acceleration for consistent screenshots.
///
/// Explicit Start()/Stop() rather than Loaded/Unloaded: the host window (FlyoutWindow) is
/// hidden with Hide(), not Closed, so a Loaded/Unloaded-driven timer would keep ticking while
/// the panel is merely off-screen.
/// </summary>
public sealed class DoomFire
{
    private const int Width = 80, Height = 30;
    private const int PaletteSteps = 37; // matches the palette built below: 36 heat levels plus black

    private readonly byte[] _heat = new byte[Width * Height];
    private readonly uint[] _pixels = new uint[Width * Height];
    private readonly uint[] _palette = BuildPalette();
    private readonly WriteableBitmap _bitmap = new(Width, Height, 96, 96, PixelFormats.Bgra32, null);
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(70) };
    private readonly Random _rng = new();

    public DoomFire(Image host)
    {
        host.Source = _bitmap;
        RenderOptions.SetBitmapScalingMode(host, BitmapScalingMode.NearestNeighbor);
        RenderOptions.SetEdgeMode(host, EdgeMode.Aliased);
        for (int x = 0; x < Width; x++) _heat[(Height - 1) * Width + x] = PaletteSteps - 1;
        _timer.Tick += (_, _) => Tick();
    }

    public void Start()
    {
        if (_timer.IsEnabled) return;
        Tick(); // paint immediately instead of waiting out the first interval
        _timer.Start();
    }

    public void Stop() => _timer.Stop();

    /// <summary>Advances several ticks at once without starting the timer. A fresh Start()
    /// only shows the seed row one step up — fine for the live panel, which keeps ticking,
    /// but a single screenshot needs fire that already looks like it's been burning a while.</summary>
    public void WarmUp(int ticks = 60)
    {
        for (int i = 0; i < ticks; i++) Tick();
    }

    private void Tick()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 1; y < Height; y++)
            {
                int src = y * Width + x;
                int decay = _rng.Next(0, 3);
                int dstX = Math.Clamp(x + _rng.Next(-1, 2), 0, Width - 1);
                _heat[(y - 1) * Width + dstX] = (byte)Math.Max(0, _heat[src] - decay);
            }
        }
        for (int i = 0; i < _heat.Length; i++) _pixels[i] = _palette[_heat[i]];
        _bitmap.WritePixels(new Int32Rect(0, 0, Width, Height), _pixels, Width * 4, 0);
    }

    /// <summary>Black through dark red, orange, yellow, to a pale flame-white at full heat —
    /// the same progression as the original effect's fixed 8-bit-era palette, just interpolated
    /// smoothly instead of hand-picked index by index.</summary>
    private static uint[] BuildPalette()
    {
        (float Pos, byte R, byte G, byte B)[] stops =
        {
            (0.00f, 0x00, 0x00, 0x00),
            (0.20f, 0x40, 0x08, 0x00),
            (0.40f, 0xA0, 0x1C, 0x00),
            (0.60f, 0xE0, 0x5A, 0x00),
            (0.80f, 0xFF, 0xB8, 0x30),
            (1.00f, 0xFF, 0xF3, 0xC8),
        };
        var palette = new uint[PaletteSteps];
        for (int i = 0; i < PaletteSteps; i++)
        {
            float t = i / (float)(PaletteSteps - 1);
            int seg = 0;
            while (seg < stops.Length - 2 && t > stops[seg + 1].Pos) seg++;
            var (p0, r0, g0, b0) = stops[seg];
            var (p1, r1, g1, b1) = stops[seg + 1];
            float local = p1 > p0 ? (t - p0) / (p1 - p0) : 0;
            byte r = (byte)(r0 + (r1 - r0) * local);
            byte g = (byte)(g0 + (g1 - g0) * local);
            byte b = (byte)(b0 + (b1 - b0) * local);
            palette[i] = 0xFF000000u | ((uint)r << 16) | ((uint)g << 8) | b; // BGRA memory order -> 0xAARRGGBB as a uint
        }
        return palette;
    }
}
