namespace Grip.Core.Monitoring;

/// <summary>
/// A fixed-size ring buffer of samples for a sparkline: cheap to append to
/// forever, always reads out oldest-to-newest for drawing.
/// </summary>
public sealed class SampleHistory
{
    private readonly double[] _values;
    private int _head;
    private int _count;

    public SampleHistory(int capacity)
    {
        if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
        Capacity = capacity;
        _values = new double[capacity];
    }

    public int Capacity { get; }

    public int Count => _count;

    public double Latest => _count == 0 ? 0 : _values[(_head - 1 + Capacity) % Capacity];

    public double Max => _count == 0 ? 0 : Values.Max();

    public void Add(double value)
    {
        _values[_head] = value;
        _head = (_head + 1) % Capacity;
        if (_count < Capacity) _count++;
    }

    /// <summary>Oldest first, newest last — the order a sparkline draws left to right.</summary>
    public IReadOnlyList<double> Values
    {
        get
        {
            var result = new double[_count];
            int start = _count < Capacity ? 0 : _head;
            for (int i = 0; i < _count; i++) result[i] = _values[(start + i) % Capacity];
            return result;
        }
    }
}
