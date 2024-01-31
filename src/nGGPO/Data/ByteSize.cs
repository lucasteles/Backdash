using System.Numerics;
using System.Runtime.CompilerServices;

namespace nGGPO.Data;

public readonly record struct ByteSize(long ByteCount)
    :
        IComparable<ByteSize>,
        IFormattable,
        IComparisonOperators<ByteSize, ByteSize, bool>,
        IAdditionOperators<ByteSize, ByteSize, ByteSize>,
        ISubtractionOperators<ByteSize, ByteSize, ByteSize>,
        IIncrementOperators<ByteSize>,
        IDecrementOperators<ByteSize>
{
    public static ByteSize One { get; } = new(1);
    public static ByteSize Zero { get; } = new(0);

    internal const ushort ByteToBits = 8;
    const double BytesToKibiByte = 1_024;
    const double BytesToMebiByte = 1_048_576;
    const double BytesToGibiByte = 1_073_741_824;
    const double BytesToKiloByte = 1_000;
    const double BytesToMegaByte = 1_000_000;
    const double BytesToGigaByte = 1_000_000_000;

    const string ByteSymbol = "B";
    const string KibiByteSymbol = "KiB";
    const string MebiByteSymbol = "MiB";
    const string GibiByteSymbol = "GiB";
    public const string KiloByteSymbol = "KB";
    public const string MegaByteSymbol = "MB";
    public const string GigaByteSymbol = "GB";

    public long Bits => ByteCount * ByteToBits;
    public double KibiBytes => ByteCount / BytesToKibiByte;
    public double MebiBytes => ByteCount / BytesToMebiByte;
    public double GibiBytes => ByteCount / BytesToGibiByte;
    public double KiloBytes => ByteCount / BytesToKiloByte;
    public double MegaBytes => ByteCount / BytesToMegaByte;
    public double GigaBytes => ByteCount / BytesToGigaByte;

    public int CompareTo(ByteSize other) => ByteCount.CompareTo(other.ByteCount);

    ReadOnlySpan<char> GetMaxBinarySymbol()
    {
        if (Math.Abs(GibiBytes) >= 1)
            return GibiByteSymbol;

        if (Math.Abs(MebiBytes) >= 1)
            return MebiByteSymbol;

        if (Math.Abs(KibiBytes) >= 1)
            return KibiByteSymbol;

        return ByteSymbol;
    }

    ReadOnlySpan<char> GetMaxDecimalSymbol()
    {
        if (Math.Abs(GigaBytes) >= 1)
            return GigaByteSymbol;

        if (Math.Abs(MegaBytes) >= 1)
            return MegaByteSymbol;

        if (Math.Abs(KiloBytes) >= 1)
            return KiloByteSymbol;

        return ByteSymbol;
    }

    public double GetValue(Measure measure) =>
        measure switch
        {
            Measure.Byte => ByteCount,
            Measure.KibiByte => KibiBytes,
            Measure.MebiByte => MebiBytes,
            Measure.GibiByte => GibiBytes,
            Measure.KiloByte => KiloBytes,
            Measure.MegaByte => MegaBytes,
            Measure.GigaByte => GigaBytes,
            _ => ByteCount,
        };

    double GetValueForSymbol(ReadOnlySpan<char> symbol) =>
        GetValue(FindMeasure(symbol));

    public string ToString(string? format, IFormatProvider? formatProvider)
    {
        const string defaultFormat = "0.##";
        const string binaryFormat = "binary";
        const string decimalFormat = "decimal";
        format ??= decimalFormat;

        if (format.Equals(binaryFormat, StringComparison.OrdinalIgnoreCase))
        {
            var maxBinarySymbol = GetMaxBinarySymbol();
            var binaryValue = GetValueForSymbol(maxBinarySymbol);
            return binaryValue.ToString($"{defaultFormat} {maxBinarySymbol}", formatProvider);
        }

        if (format.Equals(decimalFormat, StringComparison.OrdinalIgnoreCase))
        {
            var maxDecimalSymbol = GetMaxDecimalSymbol();
            var decimalValue = GetValueForSymbol(maxDecimalSymbol);
            return decimalValue.ToString($"{defaultFormat} {maxDecimalSymbol}", formatProvider);
        }

        var symbol = FindSymbol(format);

        if (symbol.IsEmpty)
            return ByteCount.ToString(format, formatProvider);

        var value = GetValueForSymbol(symbol);

        if (
            (!format.Contains('#') && !format.Contains('0'))
            || symbol.Equals(format, StringComparison.OrdinalIgnoreCase)
        )
            return value.ToString($"{defaultFormat} {symbol}", formatProvider);

        string symbolString = new(symbol);
        return value.ToString(format.Replace(symbolString, symbolString), formatProvider);
    }

    public string ToString(string? format) => ToString(format, null);
    public string ToString(Measure measure) => ToString(FindSymbol(measure));
    public override string ToString() => ToString(null, null);

    public static ByteSize SizeOf<T>() where T : struct => (ByteSize)Unsafe.SizeOf<T>();

    public static bool operator >(ByteSize left, ByteSize right) => left.ByteCount > right.ByteCount;
    public static bool operator >=(ByteSize left, ByteSize right) => left.ByteCount >= right.ByteCount;
    public static bool operator <(ByteSize left, ByteSize right) => left.ByteCount < right.ByteCount;
    public static bool operator <=(ByteSize left, ByteSize right) => left.ByteCount <= right.ByteCount;
    public static ByteSize operator ++(ByteSize value) => new(value.ByteCount + 1);
    public static ByteSize operator --(ByteSize value) => new(value.ByteCount - 1);
    public static ByteSize operator +(ByteSize left, ByteSize right) => new(left.ByteCount + right.ByteCount);
    public static ByteSize operator -(ByteSize left, ByteSize right) => new(left.ByteCount - right.ByteCount);
    public static ByteSize operator /(ByteSize left, double right) => new((long)(left.ByteCount / right));
    public static ByteSize operator /(ByteSize left, long right) => left / (double)right;
    public static ByteSize operator *(ByteSize left, long right) => new(left.ByteCount * right);
    public static ByteSize operator *(long left, ByteSize right) => new(left * right.ByteCount);
    public static explicit operator ByteSize(long value) => new(value);
    public static explicit operator ByteSize(int value) => new(value);
    public static explicit operator ByteSize(uint value) => new(value);
    public static explicit operator ByteSize(short value) => new(value);
    public static explicit operator ByteSize(ushort value) => new(value);
    public static explicit operator ByteSize(sbyte value) => new(value);
    public static explicit operator ByteSize(byte value) => new(value);

    static Measure FindMeasure(ReadOnlySpan<char> symbol) =>
        symbol switch
        {
            KibiByteSymbol => Measure.KibiByte,
            MebiByteSymbol => Measure.MebiByte,
            GibiByteSymbol => Measure.GibiByte,

            KiloByteSymbol => Measure.KiloByte,
            MegaByteSymbol => Measure.MegaByte,
            GigaByteSymbol => Measure.GigaByte,

            ByteSymbol => Measure.Byte,
            _ => Measure.Unknown,
        };

    static string FindSymbol(Measure measure) =>
        measure switch
        {
            Measure.KibiByte => KibiByteSymbol,
            Measure.MebiByte => MebiByteSymbol,
            Measure.GibiByte => GibiByteSymbol,

            Measure.KiloByte => KiloByteSymbol,
            Measure.MegaByte => MegaByteSymbol,
            Measure.GigaByte => GigaByteSymbol,

            Measure.Byte => ByteSymbol,
            _ => string.Empty,
        };

    static ReadOnlySpan<char> FindSymbol(ReadOnlySpan<char> format)
    {
        const StringComparison cmp = StringComparison.Ordinal;
        if (format.Contains(GibiByteSymbol, cmp)) return GibiByteSymbol;
        if (format.Contains(MebiByteSymbol, cmp)) return MebiByteSymbol;
        if (format.Contains(KibiByteSymbol, cmp)) return KibiByteSymbol;

        if (format.Contains(GigaByteSymbol, cmp)) return GigaByteSymbol;
        if (format.Contains(MegaByteSymbol, cmp)) return MegaByteSymbol;
        if (format.Contains(KiloByteSymbol, cmp)) return KiloByteSymbol;

        if (format.Contains(ByteSymbol, cmp)) return ByteSymbol;
        return ReadOnlySpan<char>.Empty;
    }

    public enum Measure : sbyte
    {
        Byte = 0,
        KibiByte,
        MebiByte,
        GibiByte,
        KiloByte,
        MegaByte,
        GigaByte,
        Unknown = -1,
    }
}
