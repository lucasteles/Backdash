using System.Diagnostics;
using System.Numerics;
using Backdash.Serialization.Internal;

namespace Backdash;

/// <summary>
///     Value representation of a Frame
/// </summary>
[DebuggerDisplay("{ToString()}"), Serializable]
public readonly record struct Frame :
    IComparable<Frame>,
    IComparable<int>,
    IEquatable<int>,
    IUtf8SpanFormattable,
    IFormattable,
    IComparisonOperators<Frame, Frame, bool>,
    IAdditionOperators<Frame, Frame, Frame>,
    ISubtractionOperators<Frame, Frame, Frame>,
    IIncrementOperators<Frame>,
    IDecrementOperators<Frame>,
    IComparisonOperators<Frame, int, bool>,
    IModulusOperators<Frame, int, Frame>,
    IAdditionOperators<Frame, int, Frame>,
    ISubtractionOperators<Frame, int, Frame>,
    IAdditionOperators<Frame, FrameSpan, FrameSpan>
{
    internal const sbyte NullValue = -1;

    /// <summary>Return Null frame value</summary>
    public static readonly Frame Null = new(NullValue);

    /// <summary>Return frame value <c>0</c></summary>
    public static readonly Frame Zero = new(0);

    /// <summary>Return frame value <c>1</c></summary>
    public static readonly Frame One = new(1);

    /// <summary>Returns max frame value</summary>
    public static readonly Frame MaxValue = new(int.MaxValue);

    /// <summary>Returns the <see cref="int" /> value for the current <see cref="Frame" />.</summary>
    public readonly int Number = NullValue;

    /// <summary>
    ///     Initialize new <see cref="Frame" /> for frame <paramref name="number" />.
    /// </summary>
    /// <param name="number"></param>
    public Frame(int number) => Number = number;

    /// <summary>
    ///     Returns the next frame for the current <see cref="Frame" /> value.
    /// </summary>
    public Frame Next() => new(Number + 1);

    /// <summary>
    ///     Returns the next frame after <paramref name="amount"/>
    /// </summary>
    public Frame Next(int amount) => new(Number + amount);

    /// <summary>
    ///     Returns the previous frame for the current <see cref="Frame" /> value.
    /// </summary>
    public Frame Previous() => new(Number - 1);

    /// <summary>
    ///     Returns the previous frame after <paramref name="amount"/>
    /// </summary>
    public Frame Previous(int amount) => new(Number - amount);

    /// <summary>
    ///     Returns <see langword="true" /> if the current frame is a null frame
    /// </summary>
    public bool IsNull => Number is NullValue;

    /// <inheritdoc />
    public int CompareTo(Frame other) => Number.CompareTo(other.Number);

    /// <inheritdoc />
    public int CompareTo(int other) => Number.CompareTo(other);

    /// <inheritdoc />
    public bool Equals(int other) => Number == other;

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        Number.ToString(format ?? "(Frame 0);(Frame -#)", formatProvider);

    /// <inheritdoc />
    public override string ToString() => ToString(null, null);

    /// <inheritdoc />
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

    /// <inheritdoc cref="Number" />
    public static explicit operator int(Frame frame) => frame.Number;

    /// <inheritdoc cref="Frame(int)" />
    public static explicit operator Frame(int frame) => new(frame);

    /// <summary>Returns the smaller of two <see cref="Frame" />.</summary>
    public static Frame Min(in Frame left, in Frame right) => left.Number <= right.Number ? left : right;

    /// <summary>Returns the larger of two <see cref="Frame" />.</summary>
    public static Frame Max(in Frame left, in Frame right) => left.Number >= right.Number ? left : right;

    /// <inheritdoc />
    public static bool operator >(Frame left, Frame right) => left.Number > right.Number;

    /// <inheritdoc />
    public static bool operator >=(Frame left, Frame right) => left.Number >= right.Number;

    /// <inheritdoc />
    public static bool operator <(Frame left, Frame right) => left.Number < right.Number;

    /// <inheritdoc />
    public static bool operator <=(Frame left, Frame right) => left.Number <= right.Number;

    /// <inheritdoc />
    public static Frame operator ++(Frame value) => new(value.Number + 1);

    /// <inheritdoc />
    public static Frame operator --(Frame value) => new(value.Number - 1);

    /// <inheritdoc />
    public static Frame operator +(Frame left, Frame right) => new(left.Number + right.Number);

    /// <inheritdoc />
    public static Frame operator -(Frame left, Frame right) => new(left.Number - right.Number);

    /// <inheritdoc />
    public static Frame operator %(Frame left, int right) => new(left.Number % right);

    /// <inheritdoc />
    public static Frame operator +(Frame a, int b) => new(a.Number + b);

    /// <inheritdoc />
    public static Frame operator -(Frame a, int b) => new(a.Number - b);

    /// <inheritdoc />
    public static bool operator ==(Frame left, int right) => left.Number == right;

    /// <inheritdoc />
    public static bool operator !=(Frame left, int right) => left.Number != right;

    /// <inheritdoc />
    public static bool operator >(Frame left, int right) => left.Number > right;

    /// <inheritdoc />
    public static bool operator >=(Frame left, int right) => left.Number >= right;

    /// <inheritdoc />
    public static bool operator <(Frame left, int right) => left.Number < right;

    /// <inheritdoc />
    public static bool operator <=(Frame left, int right) => left.Number <= right;

    /// <inheritdoc />
    public static FrameSpan operator +(Frame left, FrameSpan right) => right + left.Number;

    /// <summary>
    ///     Returns the absolute value of a Frame.
    /// </summary>
    public static Frame Abs(in Frame frame) => new(Math.Abs(frame.Number));

    /// <summary>
    ///     Clamps frame value to a range
    /// </summary>
    public static Frame Clamp(in Frame frame, int min, int max) => new(Math.Clamp(frame.Number, min, max));

    /// <summary>
    ///     Clamps frame value to a range
    /// </summary>
    public static Frame Clamp(in Frame frame, in Frame min, in Frame max) => Clamp(in frame, min.Number, max.Number);
}
