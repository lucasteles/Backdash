using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Backdash.Serialization.Internal;

namespace Backdash;

/// <summary>
///     Value representation of a span of frames
///     Uses the FPS defined in <seealso cref="FrameTime"/>.<see cref="FrameTime.CurrentFrameRate"/>
/// </summary>
/// <seealso cref="FrameTime.set_CurrentFrameRate"/>
[Serializable]
[DebuggerDisplay("{ToString()}")]
[StructLayout(LayoutKind.Sequential)]
public readonly record struct FrameSpan :
    IComparable<FrameSpan>,
    IUtf8SpanFormattable,
    IFormattable,
    IComparisonOperators<FrameSpan, FrameSpan, bool>,
    IAdditionOperators<FrameSpan, FrameSpan, FrameSpan>,
    ISubtractionOperators<FrameSpan, FrameSpan, FrameSpan>,
    IModulusOperators<FrameSpan, int, FrameSpan>,
    IAdditionOperators<FrameSpan, int, FrameSpan>,
    IMultiplyOperators<FrameSpan, int, FrameSpan>,
    ISubtractionOperators<FrameSpan, int, FrameSpan>,
    IAdditionOperators<FrameSpan, Frame, FrameSpan>,
    ISubtractionOperators<FrameSpan, Frame, FrameSpan>
{
    /// <summary>Return frame span of <c>0</c> frames</summary>
    public static readonly FrameSpan Zero = new(0);

    /// <summary>Return frame span of <c>1</c> frame</summary>
    public static readonly FrameSpan One = new(1);

    /// <summary>Returns max frame span value</summary>
    public static readonly FrameSpan MaxValue = new(int.MaxValue);

    /// <summary>Returns the <see cref="int" /> count of frames in the current frame span <see cref="Frame" />.</summary>
    public readonly int FrameCount = 0;

    /// <summary>
    ///     Initialize new <see cref="FrameSpan" /> for frame <paramref name="frameCount" />.
    /// </summary>
    /// <param name="frameCount"></param>
    public FrameSpan(int frameCount) => FrameCount = frameCount;

    /// <summary>Returns the time value for the current frame span in seconds.</summary>
    public double Seconds(int fps) => FrameTime.GetSeconds(FrameCount, fps);

    /// <summary>Returns the time value for the current frame span in seconds.</summary>
    public double Seconds() => FrameTime.GetSeconds(FrameCount);

    /// <summary>Returns the time value for the current frame span in <see cref="TimeSpan" />.</summary>
    public TimeSpan Duration(int fps) => FrameTime.GetDuration(FrameCount, fps);

    /// <summary>Returns the time value for the current frame span in <see cref="TimeSpan" />.</summary>
    public TimeSpan Duration() => FrameTime.GetDuration(FrameCount);

    /// <summary>Returns the value for the current frame span as a <see cref="Frame" />.</summary>
    public Frame FrameValue => new(FrameCount);

    /// <summary>
    ///     Returns frame at the time position in milliseconds
    /// </summary>
    public Frame GetFrameAtMilliSecond(double millis, int fps)
    {
        var span = FromMilliseconds(millis, fps);
        if (span.FrameCount > FrameCount)
            throw new InvalidOperationException("Out of range frame time");

        return span.FrameValue;
    }

    /// <summary>
    ///     Returns frame at the time position in milliseconds
    /// </summary>
    public Frame GetFrameAtMilliSecond(double millis)
    {
        var span = FromMilliseconds(millis);
        if (span.FrameCount > FrameCount)
            throw new InvalidOperationException("Out of range frame time");

        return span.FrameValue;
    }

    /// <summary>
    ///     Returns frame at the time position in seconds
    /// </summary>
    public Frame GetFrameAtSecond(double seconds, int fps)
    {
        var span = FromSeconds(seconds, fps);
        if (span.FrameCount > FrameCount)
            throw new InvalidOperationException("Out of range frame time");

        return span.FrameValue;
    }

    /// <summary>
    ///     Returns frame at the time position in seconds
    /// </summary>
    public Frame GetFrameAtSecond(double seconds)
    {
        var span = FromSeconds(seconds);
        if (span.FrameCount > FrameCount)
            throw new InvalidOperationException("Out of range frame time");

        return span.FrameValue;
    }

    /// <summary>
    ///     Returns frame at the timespan position
    /// </summary>
    public Frame GetFrameAt(TimeSpan duration, int fps) => GetFrameAtMilliSecond(duration.TotalMilliseconds, fps);

    /// <summary>
    ///     Returns frame at the timespan position
    /// </summary>
    public Frame GetFrameAt(TimeSpan duration) => GetFrameAtMilliSecond(duration.TotalMilliseconds);

    /// <inheritdoc />
    public int CompareTo(FrameSpan other) => FrameCount.CompareTo(other.FrameCount);

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider) =>
        FrameCount.ToString(format ?? "0 frames;-# frames", formatProvider);

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
        if (!writer.Write(FrameCount, format, provider)) return false;
        if (!writer.Write(" frames"u8)) return false;
        return true;
    }

    /// <inheritdoc cref="FrameSpan(int)" />
    public static FrameSpan Of(int frameCount) => new(frameCount);

    /// <summary>
    ///     Returns new <see cref="FrameSpan" /> for <paramref name="seconds" /> at specified <paramref name="fps" />.
    /// </summary>
    public static FrameSpan FromSeconds(double seconds, int fps) =>
        new(FrameTime.GetFrames(seconds, fps));

    /// <summary>
    ///     Returns new <see cref="FrameSpan" /> for <paramref name="seconds" />
    /// </summary>
    public static FrameSpan FromSeconds(double seconds) => new(FrameTime.GetFrames(seconds));

    /// <summary>
    ///     Returns new <see cref="FrameSpan" /> for <paramref name="duration" /> at specified <paramref name="fps" />.
    /// </summary>
    public static FrameSpan FromTimeSpan(TimeSpan duration, int fps) =>
        new(FrameTime.GetFrames(duration, fps));

    /// <summary>
    ///     Returns new <see cref="FrameSpan" /> for <paramref name="duration" />.
    /// </summary>
    public static FrameSpan FromTimeSpan(TimeSpan duration) =>
        new(FrameTime.GetFrames(duration));

    /// <summary>
    ///     Returns new <see cref="FrameSpan" /> for <paramref name="milliseconds" /> at specified <paramref name="fps" />.
    /// </summary>
    public static FrameSpan FromMilliseconds(double milliseconds, int fps) =>
        FromSeconds(milliseconds / 1000.0, fps);

    /// <summary>
    ///     Returns new <see cref="FrameSpan" /> for <paramref name="milliseconds" />
    /// </summary>
    public static FrameSpan FromMilliseconds(double milliseconds) => FromSeconds(milliseconds / 1000.0);

    /// <summary>Returns the smaller of two <see cref="FrameSpan" />.</summary>
    public static FrameSpan Min(in FrameSpan left, in FrameSpan right) => left <= right ? left : right;

    /// <summary>Returns the larger of two <see cref="FrameSpan" />.</summary>
    public static FrameSpan Max(in FrameSpan left, in FrameSpan right) => left >= right ? left : right;

    /// <inheritdoc />
    public static bool operator >(FrameSpan left, FrameSpan right) => left.FrameCount > right.FrameCount;

    /// <inheritdoc />
    public static bool operator >=(FrameSpan left, FrameSpan right) => left.FrameCount >= right.FrameCount;

    /// <inheritdoc />
    public static bool operator <(FrameSpan left, FrameSpan right) => left.FrameCount < right.FrameCount;

    /// <inheritdoc />
    public static bool operator <=(FrameSpan left, FrameSpan right) => left.FrameCount <= right.FrameCount;

    /// <inheritdoc />
    public static FrameSpan operator %(FrameSpan left, int right) => new(left.FrameCount % right);

    /// <inheritdoc />
    public static FrameSpan operator +(FrameSpan left, int right) => new(left.FrameCount + right);

    /// <inheritdoc />
    public static FrameSpan operator -(FrameSpan left, int right) => new(left.FrameCount - right);

    /// <inheritdoc />
    public static FrameSpan operator *(FrameSpan left, int right) => new(left.FrameCount * right);

    /// <inheritdoc cref="op_Multiply(Backdash.FrameSpan,int)" />
    public static FrameSpan operator *(int left, FrameSpan right) => right * left;

    /// <inheritdoc />
    public static FrameSpan operator +(FrameSpan left, Frame right) => new(left.FrameCount + right.Number);

    /// <inheritdoc />
    public static FrameSpan operator -(FrameSpan left, Frame right) => new(left.FrameCount - right.Number);

    /// <inheritdoc />
    public static FrameSpan operator +(FrameSpan left, FrameSpan right) => new(left.FrameCount + right.FrameCount);

    /// <inheritdoc />
    public static FrameSpan operator -(FrameSpan left, FrameSpan right) => new(left.FrameCount - right.FrameCount);

    /// <summary>
    ///     Returns the absolute value of a Frame.
    /// </summary>
    public static FrameSpan Abs(in FrameSpan frame) => new(Math.Abs(frame.FrameCount));

    /// <summary>
    ///     Clamps frame value to a range
    /// </summary>
    public static FrameSpan Clamp(in FrameSpan frame, int min, int max) => new(Math.Clamp(frame.FrameCount, min, max));

    /// <summary>
    ///     Clamps frame value to a range
    /// </summary>
    public static FrameSpan Clamp(in FrameSpan frame, in FrameSpan min, in FrameSpan max) =>
        Clamp(in frame, min.FrameCount, max.FrameCount);

    /// <summary>
    ///     Clamps frame value to a range
    /// </summary>
    public static FrameSpan Clamp(in FrameSpan frame, in Frame min, in Frame max) =>
        Clamp(in frame, min.Number, max.Number);
}
