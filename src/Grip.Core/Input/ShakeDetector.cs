namespace Grip.Core.Input;

/// <summary>
/// Recognizes a quick left-right wiggle while something is being dragged:
/// enough horizontal direction changes, each covering real distance, within a
/// short window. Feed it pointer samples; it answers true once per gesture.
/// </summary>
public sealed class ShakeDetector
{
    private readonly List<(long Time, int Direction, double X)> _turns = new();
    private double _lastX = double.NaN;
    private double _segmentStartX = double.NaN;
    private int _direction;
    private long _cooldownUntil;

    /// <summary>Direction changes needed (left-right-left-right = 3).</summary>
    public int RequiredTurns { get; init; } = 3;

    /// <summary>Minimum horizontal travel of each stroke, in pixels.</summary>
    public double MinStroke { get; init; } = 40;

    /// <summary>All turns must fit into this window, in milliseconds.</summary>
    public long WindowMs { get; init; } = 900;

    /// <summary>Quiet time after a detection so one wiggle fires once.</summary>
    public long CooldownMs { get; init; } = 1200;

    public void Reset()
    {
        _turns.Clear();
        _lastX = double.NaN;
        _segmentStartX = double.NaN;
        _direction = 0;
    }

    /// <summary>Adds a sample; returns true when a shake just completed.</summary>
    public bool Add(long timeMs, double x)
    {
        if (double.IsNaN(_lastX))
        {
            _lastX = x;
            _segmentStartX = x;
            return false;
        }

        double dx = x - _lastX;
        _lastX = x;
        if (Math.Abs(dx) < 1) return false;
        int direction = Math.Sign(dx);

        if (_direction == 0)
        {
            _direction = direction;
            return false;
        }

        if (direction != _direction)
        {
            double stroke = Math.Abs(x - dx - _segmentStartX);
            if (stroke >= MinStroke)
                _turns.Add((timeMs, direction, x));
            else
                _turns.Clear();
            _segmentStartX = x - dx;
            _direction = direction;
        }

        _turns.RemoveAll(t => timeMs - t.Time > WindowMs);

        if (timeMs < _cooldownUntil) return false;
        if (_turns.Count >= RequiredTurns)
        {
            _turns.Clear();
            _cooldownUntil = timeMs + CooldownMs;
            return true;
        }
        return false;
    }
}
