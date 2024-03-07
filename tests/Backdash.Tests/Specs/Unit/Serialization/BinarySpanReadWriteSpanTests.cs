using System.Runtime.CompilerServices;
using Backdash.Network;
using Backdash.Serialization.Buffer;
namespace Backdash.Tests.Specs.Unit.Serialization;
public class BinarySpanReadWriteSpanTests
{
    [PropertyTest]
    public bool SpanOfShort(short[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer, out var reader);
        writer.Write(value);
        writer.WrittenCount.Should().Be(size);
        Span<short> read = stackalloc short[value.Length];
        reader.ReadShort(read);
        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }
    [PropertyTest]
    public bool SpanOfUShort(ushort[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer, out var reader);
        writer.Write(value);
        writer.WrittenCount.Should().Be(size);
        Span<ushort> read = stackalloc ushort[value.Length];
        reader.ReadUShort(read);
        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }
    [PropertyTest]
    public bool SpanOfInt(int[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer, out var reader);
        writer.Write(value);
        writer.WrittenCount.Should().Be(size);
        Span<int> read = stackalloc int[value.Length];
        reader.ReadInt(read);
        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }
    [PropertyTest]
    public bool SpanOfUInt(uint[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer, out var reader);
        writer.Write(value);
        writer.WrittenCount.Should().Be(size);
        Span<uint> read = stackalloc uint[value.Length];
        reader.ReadUInt(read);
        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }
    [PropertyTest]
    public bool SpanOfLong(long[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer, out var reader);
        writer.Write(value);
        writer.WrittenCount.Should().Be(size);
        Span<long> read = stackalloc long[value.Length];
        reader.ReadLong(read);
        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }
    [PropertyTest]
    public bool SpanOfULong(ulong[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer, out var reader);
        writer.Write(value);
        writer.WrittenCount.Should().Be(size);
        Span<ulong> read = stackalloc ulong[value.Length];
        reader.ReadULong(read);
        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }
    [PropertyTest]
    public bool SpanOfInt128(Int128[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer, out var reader);
        writer.Write(value);
        writer.WrittenCount.Should().Be(size);
        Span<Int128> read = stackalloc Int128[value.Length];
        reader.ReadInt128(read);
        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }
    [PropertyTest]
    public bool SpanOfUInt128(UInt128[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer, out var reader);
        writer.Write(value);
        writer.WrittenCount.Should().Be(size);
        Span<UInt128> read = stackalloc UInt128[value.Length];
        reader.ReadUInt128(read);
        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }
    [PropertyTest]
    public bool SpanOfByte(byte[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer, out var reader);
        writer.Write(value);
        writer.WrittenCount.Should().Be(size);
        Span<byte> read = stackalloc byte[value.Length];
        reader.ReadByte(read);
        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }
    [PropertyTest]
    public bool SpanOfSByte(sbyte[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer, out var reader);
        writer.Write(value);
        writer.WrittenCount.Should().Be(size);
        Span<sbyte> read = stackalloc sbyte[value.Length];
        reader.ReadSByte(read);
        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }
    [PropertyTest]
    public bool SpanOfChars(char[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer, out var reader);
        writer.Write(value);
        writer.WrittenCount.Should().Be(size);
        Span<char> read = stackalloc char[value.Length];
        reader.ReadChar(read);
        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }
    [PropertyTest]
    public bool SpanOBooleans(bool[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer, out var reader);
        writer.Write(value);
        writer.WrittenCount.Should().Be(size);
        Span<bool> read = stackalloc bool[value.Length];
        reader.ReadBool(read);
        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }
    static int writeOffset;
    static int readOffset;
    static int Setup<T>(
        IReadOnlyCollection<T> values,
        Endianness endianness,
        out BinarySpanWriter writer,
        out BinarySpanReader reader) where T : struct
    {
        var size = Unsafe.SizeOf<T>() * values.Count;
        Span<byte> buffer = new byte[size];
        writeOffset = 0;
        readOffset = 0;
        writer = new(buffer, ref writeOffset)
        {
            Endianness = endianness,
        };
        reader = new(buffer, ref readOffset)
        {
            Endianness = endianness,
        };
        return size;
    }
}
