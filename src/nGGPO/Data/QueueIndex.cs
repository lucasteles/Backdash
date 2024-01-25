using nGGPO.Utils;

namespace nGGPO.Data;

readonly record struct QueueIndex : IComparable<Frame>
{
    public int Value { get; }

    public QueueIndex(int value)
    {
        ExceptionHelper.ThrowIfArgumentOutOfBounds(value, min: 1);
        Value = value;
    }

    public int CompareTo(Frame other) => Value.CompareTo(other.Number);

    public override string ToString() => Value.ToString();

    public static QueueIndex operator +(QueueIndex a, QueueIndex b) => new(a.Value + b.Value);
    public static QueueIndex operator +(QueueIndex a, int b) => new(a.Value + b);
    public static QueueIndex operator +(int a, QueueIndex b) => new(a + b.Value);
    public static QueueIndex operator ++(QueueIndex queue) => new(queue.Value + 1);

    public static explicit operator int(QueueIndex queue) => queue.Value;
    public static explicit operator QueueIndex(int queue) => new(queue);
}