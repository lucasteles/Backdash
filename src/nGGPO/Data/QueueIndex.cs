using System.Numerics;
using nGGPO.Core;

namespace nGGPO.Data;

readonly record struct QueueIndex :
    IComparable<QueueIndex>,
    IFormattable,
    IComparisonOperators<QueueIndex, QueueIndex, bool>,
    IAdditionOperators<QueueIndex, QueueIndex, QueueIndex>,
    IAdditionOperators<QueueIndex, int, QueueIndex>,
    IIncrementOperators<QueueIndex>
{
    public int Value { get; }

    public QueueIndex(int value)
    {
        ThrowHelpers.ThrowIfArgumentOutOfBounds(value, min: 1);
        Value = value;
    }

    public int CompareTo(QueueIndex other) => Value.CompareTo(other.Value);

    public override string ToString() => ToString(null, null);
    public string ToString(string? format, IFormatProvider? formatProvider) => Value.ToString(format, formatProvider);

    public static QueueIndex operator +(QueueIndex a, QueueIndex b) => new(a.Value + b.Value);
    public static QueueIndex operator +(QueueIndex a, int b) => new(a.Value + b);
    public static QueueIndex operator ++(QueueIndex queue) => new(queue.Value + 1);

    public static explicit operator int(QueueIndex queue) => queue.Value;
    public static explicit operator QueueIndex(int queue) => new(queue);

    public static bool operator >(QueueIndex left, QueueIndex right) => left.Value > right.Value;
    public static bool operator >=(QueueIndex left, QueueIndex right) => left.Value >= right.Value;
    public static bool operator <(QueueIndex left, QueueIndex right) => left.Value < right.Value;
    public static bool operator <=(QueueIndex left, QueueIndex right) => left.Value <= right.Value;
}
