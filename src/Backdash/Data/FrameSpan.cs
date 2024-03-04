using System.Diagnostics;
using System.Numerics;
using Backdash.Serialization.Buffer;

namespace Backdash.Data;

[DebuggerDisplay("{ToString()}")]
public readonly record struct FrameSpan :
    IComparable<FrameSpan>,
    IUtf8SpanFormattable,
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
    public const short DefaultFramesPerSecond = 60;

    public static readonly FrameSpan Zero = new(0);
    public static readonly FrameSpan One = new(1);
    public static readonly FrameSpan MaxValue = new(int.MaxValue);
    public static readonly FrameSpan MinValue = new(int.MinValue);

    public readonly int FrameCount = 0;
    public FrameSpan(int frameCount) => FrameCount = frameCount;

    public double Seconds(short fps = DefaultFramesPerSecond) => InSeconds(FrameCount, fps);
    public TimeSpan Duration(short fps = DefaultFramesPerSecond) => GetDuration(FrameCount, fps);
    public Frame Value => new(FrameCount);
    public int CompareTo(FrameSpan other) => FrameCount.CompareTo(other.FrameCount);

    public string ToString(string? format, IFormatProvider? formatProvider) =>
        FrameCount.ToString(format ?? "0 frames;-# frames", formatProvider);

    public override string ToString() => ToString(null, null);

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

    public static FrameSpan FromSeconds(double seconds, short fps = DefaultFramesPerSecond) =>
        new((int)(seconds * fps));

    public static FrameSpan FromTimeSpan(TimeSpan time, short fps = DefaultFramesPerSecond) =>
        FromSeconds(time.TotalSeconds, fps);

    public static FrameSpan FromMilliseconds(double milliseconds, short fps = DefaultFramesPerSecond) =>
        FromSeconds(milliseconds / 1000, fps);

    public static double InSeconds(int frameCount, short fps = DefaultFramesPerSecond) => frameCount / (double)fps;

    public static TimeSpan GetDuration(int frameCount, short fps = DefaultFramesPerSecond) =>
        TimeSpan.FromSeconds(InSeconds(frameCount, fps));

    public static FrameSpan Min(in FrameSpan left, in FrameSpan right) => left <= right ? left : right;
    public static FrameSpan Max(in FrameSpan left, in FrameSpan right) => left >= right ? left : right;

    public static bool operator >(FrameSpan left, FrameSpan right) => left.FrameCount > right.FrameCount;
    public static bool operator >=(FrameSpan left, FrameSpan right) => left.FrameCount >= right.FrameCount;
    public static bool operator <(FrameSpan left, FrameSpan right) => left.FrameCount < right.FrameCount;
    public static bool operator <=(FrameSpan left, FrameSpan right) => left.FrameCount <= right.FrameCount;

    public static FrameSpan operator %(FrameSpan left, int right) => new(left.FrameCount % right);
    public static FrameSpan operator +(FrameSpan left, int right) => new(left.FrameCount + right);
    public static FrameSpan operator -(FrameSpan left, int right) => new(left.FrameCount - right);
    public static FrameSpan operator *(FrameSpan left, int right) => new(left.FrameCount * right);
    public static FrameSpan operator *(int left, FrameSpan right) => right * left;

    public static FrameSpan operator +(FrameSpan left, Frame right) => new(left.FrameCount + right.Number);
    public static FrameSpan operator -(FrameSpan left, Frame right) => new(left.FrameCount - right.Number);
    public static FrameSpan operator +(FrameSpan left, FrameSpan right) => new(left.FrameCount + right.FrameCount);
    public static FrameSpan operator -(FrameSpan left, FrameSpan right) => new(left.FrameCount - right.FrameCount);
}
