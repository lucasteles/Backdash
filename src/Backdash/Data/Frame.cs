using System.Diagnostics;
using System.Numerics;
using Backdash.Serialization.Buffer;

namespace Backdash.Data;

[DebuggerDisplay("{ToString()}")]
readonly record struct Frame :
    IComparable<Frame>,
    IComparable<int>,
    IEquatable<int>,
    IUtf8SpanFormattable,
    IComparisonOperators<Frame, Frame, bool>,
    IAdditionOperators<Frame, Frame, Frame>,
    ISubtractionOperators<Frame, Frame, Frame>,
    IIncrementOperators<Frame>,
    IDecrementOperators<Frame>,
    IComparisonOperators<Frame, int, bool>,
    IModulusOperators<Frame, int, Frame>,
    IAdditionOperators<Frame, int, Frame>,
    ISubtractionOperators<Frame, int, Frame>
{
    public const sbyte NullValue = -1;
    public static readonly Frame Null = new(NullValue);
    public static readonly Frame Zero = new(0);
    public static readonly Frame MaxValue = new(int.MaxValue);
    public readonly int Number = NullValue;
    public Frame(int number) => Number = number;
    public Frame Next() => new(Number + 1);
    public Frame Previous() => new(Number - 1);
    public bool IsNull => Number is NullValue;
    public bool IsNotNull => !IsNull;

    public int CompareTo(Frame other) => Number.CompareTo(other.Number);
    public int CompareTo(int other) => Number.CompareTo(other);
    public bool Equals(int other) => Number == other;

    public override string ToString() => Number.ToString();

    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Number.ToString(format ?? "Frame #;Frame -#", formatProvider);

    public bool TryFormat(
        Span<byte> utf8Destination, out int bytesWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? provider
    )
    {
        bytesWritten = 0;
        Utf8StringWriter writer = new(in utf8Destination, ref bytesWritten);
        return writer.Write(Number, format);
    }

    public static explicit operator int(Frame frame) => frame.Number;
    public static explicit operator Frame(int frame) => new(frame);

    public static Frame Min(in Frame val1, in Frame val2) => val1 <= val2 ? val1 : val2;
    public static Frame Max(in Frame val1, in Frame val2) => val1 >= val2 ? val1 : val2;

    public static bool operator >(Frame left, Frame right) => left.Number > right.Number;
    public static bool operator >=(Frame left, Frame right) => left.Number >= right.Number;
    public static bool operator <(Frame left, Frame right) => left.Number < right.Number;
    public static bool operator <=(Frame left, Frame right) => left.Number <= right.Number;
    public static Frame operator ++(Frame value) => new(value.Number + 1);
    public static Frame operator --(Frame value) => new(value.Number - 1);
    public static Frame operator +(Frame left, Frame right) => new(left.Number + right.Number);
    public static Frame operator -(Frame left, Frame right) => new(left.Number - right.Number);

    public static Frame operator %(Frame left, int right) => new(left.Number % right);
    public static Frame operator +(Frame a, int b) => new(a.Number + b);
    public static Frame operator -(Frame a, int b) => new(a.Number - b);

    public static bool operator ==(Frame left, int right) => left.Number == right;
    public static bool operator !=(Frame left, int right) => left.Number != right;
    public static bool operator >(Frame left, int right) => left.Number > right;
    public static bool operator >=(Frame left, int right) => left.Number >= right;
    public static bool operator <(Frame left, int right) => left.Number < right;
    public static bool operator <=(Frame left, int right) => left.Number <= right;
}
