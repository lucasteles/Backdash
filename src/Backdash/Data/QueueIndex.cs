using System.Numerics;
using Backdash.Core;

namespace Backdash.Data;

readonly record struct QueueIndex :
    IComparable<QueueIndex>,
    IFormattable,
    IComparisonOperators<QueueIndex, QueueIndex, bool>,
    IAdditionOperators<QueueIndex, QueueIndex, QueueIndex>,
    IAdditionOperators<QueueIndex, int, QueueIndex>,
    IIncrementOperators<QueueIndex>
{
    public int Number { get; }

    public QueueIndex(int number)
    {
        ThrowHelpers.ThrowIfArgumentOutOfBounds(number, min: 1);
        Number = number;
    }

    public int CompareTo(QueueIndex other) => Number.CompareTo(other.Number);

    public override string ToString() => ToString(null, null);
    public string ToString(string? format, IFormatProvider? formatProvider) => Number.ToString(format, formatProvider);

    public static QueueIndex operator +(QueueIndex a, QueueIndex b) => new(a.Number + b.Number);
    public static QueueIndex operator +(QueueIndex a, int b) => new(a.Number + b);
    public static QueueIndex operator ++(QueueIndex queue) => new(queue.Number + 1);

    public static explicit operator int(QueueIndex queue) => queue.Number;
    public static explicit operator QueueIndex(int queue) => new(queue);

    public static bool operator >(QueueIndex left, QueueIndex right) => left.Number > right.Number;
    public static bool operator >=(QueueIndex left, QueueIndex right) => left.Number >= right.Number;
    public static bool operator <(QueueIndex left, QueueIndex right) => left.Number < right.Number;
    public static bool operator <=(QueueIndex left, QueueIndex right) => left.Number <= right.Number;
}
