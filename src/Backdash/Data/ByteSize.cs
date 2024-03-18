using System.Numerics;
using Backdash.Serialization.Buffer;

namespace Backdash.Data;

/// <summary>
/// Represents a byte size value
/// </summary>
public readonly record struct ByteSize(long ByteCount)
    :
        IComparable<ByteSize>,
        IFormattable,
        IUtf8SpanFormattable,
        IComparisonOperators<ByteSize, ByteSize, bool>,
        IAdditionOperators<ByteSize, ByteSize, ByteSize>,
        ISubtractionOperators<ByteSize, ByteSize, ByteSize>,
        IDivisionOperators<ByteSize, long, ByteSize>,
        IDivisionOperators<ByteSize, double, ByteSize>,
        IMultiplyOperators<ByteSize, long, ByteSize>,
        IIncrementOperators<ByteSize>,
        IDecrementOperators<ByteSize>
{
    /// <summary>Gets the byte value <c>1</c>.</summary>
    public static ByteSize One { get; } = new(1);

    /// <summary>Gets the byte value <c>0</c>.</summary>
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

    /// <summary>Gets the number of KibiBytes represented by this object.</summary>
    public double KibiBytes => ByteCount / BytesToKibiByte;

    /// <summary>Gets the number of MebiBytes represented by this object.</summary>
    public double MebiBytes => ByteCount / BytesToMebiByte;

    /// <summary>Gets the number of GibiBytes represented by this object.</summary>
    public double GibiBytes => ByteCount / BytesToGibiByte;

    /// <summary>Gets the number of TebiBytes represented by this object.</summary>
    public double TebiBytes => ByteCount / BytesToTebiByte;

    /// <summary>Gets the number of KiloBytes represented by this object.</summary>
    public double KiloBytes => ByteCount / BytesToKiloByte;

    /// <summary>Gets the number of MegaBytes represented by this object.</summary>
    public double MegaBytes => ByteCount / BytesToMegaByte;

    /// <summary>Gets the number of GigaBytes represented by this object.</summary>
    public double GigaBytes => ByteCount / BytesToGigaByte;

    /// <summary>Gets the number of TeraBytes represented by this object.</summary>
    public double TeraBytes => ByteCount / BytesToTeraByte;

    /// <inheritdoc />
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

    double GetValue(Measure measure) =>
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
    const string defaultFormat = "0.##";
    const string binaryFormat = "binary";
    const string decimalFormat = "decimal";

    /// <inheritdoc />
    public string ToString(string? format, IFormatProvider? formatProvider)
    {
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
        return value.ToString(
            format.Replace(symbolString, symbolString, StringComparison.OrdinalIgnoreCase),
            formatProvider
        );
    }

    /// <summary>
    /// Returns the string representation for the current byte size
    /// </summary>
    public override string ToString() => ToString(null, null);

    /// <inheritdoc cref="ToString(string?,System.IFormatProvider?)" />
    public string ToString(string? format) => ToString(format, null);

    /// <summary>
    /// Returns the string representation for the current byte size as <paramref name="measure"/>
    /// </summary>
    /// <para name="measure">The unit of measure conversion</para>
    public string ToString(Measure measure) => ToString(MeasureToSymbol(measure));

    /// <inheritdoc cref="ToString(string?,System.IFormatProvider?)" />
    public bool TryFormat(
        Span<byte> utf8Destination,
        out int bytesWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? provider)
    {
        bytesWritten = 0;
        Utf8StringWriter writer = new(in utf8Destination, ref bytesWritten);
        format = format.IsEmpty ? decimalFormat : format;
        if (format.Equals(binaryFormat, StringComparison.OrdinalIgnoreCase))
        {
            var maxBinarySymbol = GetMaxBinarySymbol();
            var binaryValue = GetValueForSymbol(maxBinarySymbol);
            writer.Write(binaryValue, defaultFormat, provider);
            writer.Write(" "u8);
            return writer.WriteChars(maxBinarySymbol);
        }

        if (format.Equals(decimalFormat, StringComparison.OrdinalIgnoreCase))
        {
            var maxDecimalSymbol = GetMaxDecimalSymbol();
            var decimalValue = GetValueForSymbol(maxDecimalSymbol);
            writer.Write(decimalValue, defaultFormat, provider);
            writer.Write(" "u8);
            return writer.WriteChars(maxDecimalSymbol);
        }

        var symbol = FindSymbol(format);
        if (symbol.IsEmpty)
            return writer.Write(ByteCount, format, provider);
        var value = GetValueForSymbol(symbol);
        if (
            (!format.Contains('#') && !format.Contains('0'))
            || symbol.Equals(format, StringComparison.OrdinalIgnoreCase)
        )
        {
            writer.Write(value, defaultFormat, provider);
            writer.Write(" "u8);
            return writer.WriteChars(symbol);
        }

        return writer.Write(value, format, provider);
    }

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
        return [];
    }

    /// <inheritdoc />
    public static bool operator >(ByteSize left, ByteSize right) => left.ByteCount > right.ByteCount;

    /// <inheritdoc />
    public static bool operator >=(ByteSize left, ByteSize right) => left.ByteCount >= right.ByteCount;

    /// <inheritdoc />
    public static bool operator <(ByteSize left, ByteSize right) => left.ByteCount < right.ByteCount;

    /// <inheritdoc />
    public static bool operator <=(ByteSize left, ByteSize right) => left.ByteCount <= right.ByteCount;

    /// <inheritdoc />
    public static ByteSize operator ++(ByteSize value) => new(value.ByteCount + 1);

    /// <inheritdoc />
    public static ByteSize operator --(ByteSize value) => new(value.ByteCount - 1);

    /// <inheritdoc />
    public static ByteSize operator +(ByteSize left, ByteSize right) => new(left.ByteCount + right.ByteCount);

    /// <inheritdoc />
    public static ByteSize operator -(ByteSize left, ByteSize right) => new(left.ByteCount - right.ByteCount);

    /// <inheritdoc />
    public static ByteSize operator /(ByteSize left, double right) => new((long)(left.ByteCount / right));

    /// <inheritdoc />
    public static ByteSize operator /(ByteSize left, long right) => left / (double)right;

    /// <inheritdoc />
    public static ByteSize operator *(ByteSize left, long right) => new(left.ByteCount * right);

    /// <inheritdoc cref="op_Multiply(ByteSize,long)" />
    public static ByteSize operator *(long left, ByteSize right) => new(left * right.ByteCount);

    /// <summary>
    /// Returns new <see cref="ByteSize"/> with <paramref name="value"/> bytes
    /// </summary>
    /// <param name="value"></param>
    public static explicit operator ByteSize(long value) => new(value);

    /// <inheritdoc cref="op_Explicit(long)" />
    public static explicit operator ByteSize(int value) => new(value);

    /// <inheritdoc cref="op_Explicit(long)" />
    public static explicit operator ByteSize(uint value) => new(value);

    /// <inheritdoc cref="op_Explicit(long)" />
    public static explicit operator ByteSize(short value) => new(value);

    /// <inheritdoc cref="op_Explicit(long)" />
    public static explicit operator ByteSize(ushort value) => new(value);

    /// <inheritdoc cref="op_Explicit(long)" />
    public static explicit operator ByteSize(sbyte value) => new(value);

    /// <inheritdoc cref="op_Explicit(long)" />
    public static explicit operator ByteSize(byte value) => new(value);

    /// <summary>
    /// Returns new <see cref="ByteSize"/> with <paramref name="value"/> bytes
    /// </summary>
    /// <param name="value">Number of bytes</param>
    public static ByteSize FromBytes(long value) => new(value);

    /// <summary>
    /// Returns new <see cref="ByteSize"/> with <paramref name="value"/> kilo-bytes
    /// </summary>
    /// <param name="value">Number of kilo-bytes</param>
    public static ByteSize FromKiloByte(double value) => new((long)(value * BytesToKiloByte));

    /// <summary>
    /// Returns new <see cref="ByteSize"/> with <paramref name="value"/> mega-bytes
    /// </summary>
    /// <param name="value">Number of mega-bytes</param>
    public static ByteSize FromMegaBytes(double value) => new((long)(value * BytesToMegaByte));

    /// <summary>
    /// Returns new <see cref="ByteSize"/> with <paramref name="value"/> giga-bytes
    /// </summary>
    /// <param name="value">Number of giga-bytes</param>
    public static ByteSize FromGigaBytes(double value) => new((long)(value * BytesToGigaByte));

    /// <summary>
    /// Returns new <see cref="ByteSize"/> with <paramref name="value"/> tera-bytes
    /// </summary>
    /// <param name="value">Number of tera-bytes</param>
    public static ByteSize FromTeraBytes(double value) => new((long)(value * BytesToTeraByte));

    /// <summary>
    /// Returns new <see cref="ByteSize"/> with <paramref name="value"/> kibi-bytes
    /// </summary>
    /// <param name="value">Number of kibi-bytes</param>
    public static ByteSize FromKibiBytes(double value) => new((long)(value * BytesToKibiByte));

    /// <summary>
    /// Returns new <see cref="ByteSize"/> with <paramref name="value"/> mebi-bytes
    /// </summary>
    /// <param name="value">Number of mebi-bytes</param>
    public static ByteSize FromMebiBytes(double value) => new((long)(value * BytesToMebiByte));

    /// <summary>
    /// Returns new <see cref="ByteSize"/> with <paramref name="value"/> gibi-bytes
    /// </summary>
    /// <param name="value">Number of gibi-bytes</param>
    public static ByteSize FromGibiBytes(double value) => new((long)(value * BytesToGibiByte));

    /// <summary>
    /// Returns new <see cref="ByteSize"/> with <paramref name="value"/> gibi-bytes
    /// </summary>
    /// <param name="value">Number of tebi-bytes</param>
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

    /// <summary>
    /// Unit of measure for <see cref="ByteSize"/>
    /// </summary>
    public enum Measure : sbyte
    {
        /// <summary>
        /// Byte
        /// </summary>
        Byte = 0,

        /// <summary>
        /// 1KiB == 1024 bytes
        /// </summary>
        KibiByte,

        /// <summary>
        /// 1MiB == 1_048_576 bytes
        /// </summary>
        MebiByte,

        /// <summary>
        /// 1GiB == 1_073_741_824 bytes
        /// </summary>
        GibiByte,

        /// <summary>
        /// 1TiB == 1_099_511_627_776 bytes
        /// </summary>
        TebiByte,

        /// <summary>
        /// 1KB == 1000 bytes
        /// </summary>
        KiloByte,

        /// <summary>
        /// 1MB == 1_000_000 bytes
        /// </summary>
        MegaByte,

        /// <summary>
        /// 1GB == 1_000_000_000 bytes
        /// </summary>
        GigaByte,

        /// <summary>
        /// 1TB == 1_000_000_000_000 bytes
        /// </summary>
        TeraByte,

        /// <summary>
        /// Unknown unit of measure
        /// </summary>
        Unknown = -1,
    }
}
