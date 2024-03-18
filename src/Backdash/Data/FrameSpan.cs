using System.Diagnostics;
using System.Numerics;
using Backdash.Serialization.Buffer;

namespace Backdash.Data;

/// <summary>
/// Value representation of a span of frames
/// </summary>
[DebuggerDisplay("{ToString()}")]
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
    /// <summary>Default FPS(frames per second)<value>60</value></summary>
    public const short DefaultFramesPerSecond = 60;


    /// <summary>Return frame span of <c>0</c> frames</summary>
    public static readonly FrameSpan Zero = new(0);

    /// <summary>Return frame span of <c>1</c> frame</summary>
    public static readonly FrameSpan One = new(1);

    /// <summary>Returns max frame span value</summary>
    public static readonly FrameSpan MaxValue = new(int.MaxValue);

    /// <summary>Returns the <see cref="System.Int32"/> count of frames in the current frame span <see cref="Frame"/>.</summary>
    public readonly int FrameCount = 0;

    /// <summary>
    /// Initialize new <see cref="FrameSpan"/> for frame <paramref name="frameCount" />.
    /// </summary>
    /// <param name="frameCount"></param>
    public FrameSpan(int frameCount) => FrameCount = frameCount;

    /// <summary>Returns the time value for the current frame span in seconds</summary>.
    public double Seconds(short fps = DefaultFramesPerSecond) => InSeconds(FrameCount, fps);

    /// <summary>Returns the time value for the current frame span in <see cref="TimeSpan"/>.</summary>
    public TimeSpan Duration(short fps = DefaultFramesPerSecond) => GetDuration(FrameCount, fps);

    /// <summary>Returns the value for the current frame span as a <see cref="Frame"/>.</summary>
    public Frame Value => new(FrameCount);

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
        if (!writer.Write(FrameCount, format)) return false;
        if (!writer.Write(" frames"u8)) return false;
        return true;
    }

    /// <inheritdoc cref="FrameSpan(int)"/>
    public static FrameSpan Of(int frameCount) => new(frameCount);

    /// <summary>
    /// Returns new <see cref="FrameSpan"/> for <paramref name="seconds"/> at specified <paramref name="fps"/>.
    /// </summary>
    public static FrameSpan FromSeconds(double seconds, short fps = DefaultFramesPerSecond) =>
        new((int)(seconds * fps));

    /// <summary>
    /// Returns new <see cref="FrameSpan"/> for <paramref name="time"/> at specified <paramref name="fps"/>.
    /// </summary>
    public static FrameSpan FromTimeSpan(TimeSpan time, short fps = DefaultFramesPerSecond) =>
        FromSeconds(time.TotalSeconds, fps);

    /// <summary>
    /// Returns new <see cref="FrameSpan"/> for <paramref name="milliseconds"/> at specified <paramref name="fps"/>.
    /// </summary>
    public static FrameSpan FromMilliseconds(double milliseconds, short fps = DefaultFramesPerSecond) =>
        FromSeconds(milliseconds / 1000, fps);

    /// <summary>
    /// Returns <see cref="System.Double"/> seconds for <paramref name="frameCount"/> at specified <paramref name="fps"/>.
    /// </summary>
    public static double InSeconds(int frameCount, short fps = DefaultFramesPerSecond) => frameCount / (double)fps;

    /// <summary>
    /// Returns <see cref="System.TimeSpan"/> for <paramref name="frameCount"/> at specified <paramref name="fps"/>.
    /// </summary>
    public static TimeSpan GetDuration(int frameCount, short fps = DefaultFramesPerSecond) =>
        TimeSpan.FromSeconds(InSeconds(frameCount, fps));

    /// <summary>Returns the smaller of two <see cref="FrameSpan"/>.</summary>
    public static FrameSpan Min(in FrameSpan left, in FrameSpan right) => left <= right ? left : right;

    /// <summary>Returns the larger of two <see cref="FrameSpan"/>.</summary>
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

    /// <inheritdoc cref="op_Multiply(Backdash.Data.FrameSpan,int)"/>
    public static FrameSpan operator *(int left, FrameSpan right) => right * left;

    /// <inheritdoc />
    public static FrameSpan operator +(FrameSpan left, Frame right) => new(left.FrameCount + right.Number);

    /// <inheritdoc />
    public static FrameSpan operator -(FrameSpan left, Frame right) => new(left.FrameCount - right.Number);

    /// <inheritdoc />
    public static FrameSpan operator +(FrameSpan left, FrameSpan right) => new(left.FrameCount + right.FrameCount);

    /// <inheritdoc />
    public static FrameSpan operator -(FrameSpan left, FrameSpan right) => new(left.FrameCount - right.FrameCount);
}
