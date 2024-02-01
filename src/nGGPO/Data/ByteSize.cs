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
    const double BytesToTeraByte = 1_000_000_000_000;
    const double BytesToTebiByte = 1_099_511_627_776;

    const string ByteSymbol = "B";
    const string KibiByteSymbol = "KiB";
    const string MebiByteSymbol = "MiB";
    const string GibiByteSymbol = "GiB";
    const string TebiByteSymbol = "TiB";

    const string KiloByteSymbol = "KB";
    const string MegaByteSymbol = "MB";
    const string GigaByteSymbol = "GB";
    const string TeraByteSymbol = "TB";

    public double KibiBytes => ByteCount / BytesToKibiByte;
    public double MebiBytes => ByteCount / BytesToMebiByte;
    public double GibiBytes => ByteCount / BytesToGibiByte;

    public double TebiBytes => ByteCount / BytesToTebiByte;

    public double KiloBytes => ByteCount / BytesToKiloByte;
    public double MegaBytes => ByteCount / BytesToMegaByte;
    public double GigaBytes => ByteCount / BytesToGigaByte;

    public double TeraBytes => ByteCount / BytesToTeraByte;

    public int CompareTo(ByteSize other) => ByteCount.CompareTo(other.ByteCount);

    ReadOnlySpan<char> GetMaxBinarySymbol()
    {
        if (Math.Abs(TebiBytes) >= 1)
            return TebiByteSymbol;

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
        if (Math.Abs(TeraBytes) >= 1)
            return TeraByteSymbol;

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
            Measure.TebiByte => TebiBytes,
            Measure.KiloByte => KiloBytes,
            Measure.MegaByte => MegaBytes,
            Measure.GigaByte => GigaBytes,
            Measure.TeraByte => TeraBytes,
            _ => ByteCount,
        };

    double GetValueForSymbol(ReadOnlySpan<char> symbol) => GetValue(SymbolToMeasure(symbol));

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

        static ReadOnlySpan<char> FindSymbol(ReadOnlySpan<char> str)
        {
            const StringComparison cmp = StringComparison.Ordinal;
            if (str.Contains(KibiByteSymbol, cmp)) return KibiByteSymbol;
            if (str.Contains(MebiByteSymbol, cmp)) return MebiByteSymbol;
            if (str.Contains(GibiByteSymbol, cmp)) return GibiByteSymbol;
            if (str.Contains(TebiByteSymbol, cmp)) return TebiByteSymbol;

            if (str.Contains(KiloByteSymbol, cmp)) return KiloByteSymbol;
            if (str.Contains(MegaByteSymbol, cmp)) return MegaByteSymbol;
            if (str.Contains(GigaByteSymbol, cmp)) return GigaByteSymbol;
            if (str.Contains(TeraByteSymbol, cmp)) return TeraByteSymbol;

            if (str.Contains(ByteSymbol, cmp)) return ByteSymbol;
            return ReadOnlySpan<char>.Empty;
        }
    }

    public string ToString(string? format) => ToString(format, null);
    public string ToString(Measure measure) => ToString(MeasureToSymbol(measure));
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
    public static ByteSize FromBytes(long value) => new(value);
    public static ByteSize FromKiloByte(double value) => new((long)(value * BytesToKiloByte));
    public static ByteSize FromMegaBytes(double value) => new((long)(value * BytesToMegaByte));
    public static ByteSize FromGigaBytes(double value) => new((long)(value * BytesToGigaByte));
    public static ByteSize FromTeraBytes(double value) => new((long)(value * BytesToTeraByte));
    public static ByteSize FromKibiBytes(double value) => new((long)(value * BytesToKibiByte));
    public static ByteSize FromMebiBytes(double value) => new((long)(value * BytesToMebiByte));
    public static ByteSize FromGibiBytes(double value) => new((long)(value * BytesToGibiByte));
    public static ByteSize FromTebiBytes(double value) => new((long)(value * BytesToTebiByte));

    static Measure SymbolToMeasure(ReadOnlySpan<char> symbol) =>
        symbol switch
        {
            KibiByteSymbol => Measure.KibiByte,
            MebiByteSymbol => Measure.MebiByte,
            GibiByteSymbol => Measure.GibiByte,
            TebiByteSymbol => Measure.TebiByte,

            KiloByteSymbol => Measure.KiloByte,
            MegaByteSymbol => Measure.MegaByte,
            GigaByteSymbol => Measure.GigaByte,
            TeraByteSymbol => Measure.TeraByte,

            ByteSymbol => Measure.Byte,
            _ => Measure.Unknown,
        };

    static string MeasureToSymbol(Measure measure) =>
        measure switch
        {
            Measure.KibiByte => KibiByteSymbol,
            Measure.MebiByte => MebiByteSymbol,
            Measure.GibiByte => GibiByteSymbol,
            Measure.TebiByte => TebiByteSymbol,

            Measure.KiloByte => KiloByteSymbol,
            Measure.MegaByte => MegaByteSymbol,
            Measure.GigaByte => GigaByteSymbol,
            Measure.TeraByte => TeraByteSymbol,

            Measure.Byte => ByteSymbol,
            _ => string.Empty,
        };

    public enum Measure : sbyte
    {
        Byte = 0,
        KibiByte,
        MebiByte,
        GibiByte,
        TebiByte,
        KiloByte,
        MegaByte,
        GigaByte,
        TeraByte,
        Unknown = -1,
    }
}
