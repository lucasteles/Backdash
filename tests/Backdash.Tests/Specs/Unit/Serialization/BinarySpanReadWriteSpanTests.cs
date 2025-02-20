using System.Runtime.CompilerServices;
using Backdash.Network;
using Backdash.Serialization;
using Backdash.Tests.TestUtils;
using Backdash.Tests.TestUtils.Types;

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
        reader.ReadInt16(read);
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
        reader.ReadUInt16(read);
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
        reader.ReadInt32(read);
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
        reader.ReadUInt32(read);
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
        reader.ReadInt64(read);
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
        reader.ReadUInt64(read);
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
        reader.ReadBoolean(read);
        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }

    [PropertyTest]
    public bool SpanOfUtf8(NonEmptyString input, Endianness endianness)
    {
        var value = input.Item;
        var utf8Bytes = System.Text.Encoding.UTF8.GetBytes(value);

        var size = Setup(utf8Bytes, endianness, out var writer, out var reader);
        writer.WriteUtf8String(value);
        writer.WrittenCount.Should().Be(size);

        Span<char> read = stackalloc char[value.Length];
        reader.ReadUtf8String(read);

        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }

    [PropertyTest]
    public bool SpanOfUtf8Bytes(NonEmptyString input, Endianness endianness)
    {
        var value = input.Item;
        var utf8Size = System.Text.Encoding.UTF8.GetByteCount(value);
        var utf8Bytes = System.Text.Encoding.UTF8.GetBytes(value);

        var size = Setup(utf8Bytes, endianness, out var writer, out var reader);
        writer.WriteUtf8String(value);
        writer.WrittenCount.Should().Be(size);

        Span<byte> read = stackalloc byte[utf8Size];
        reader.ReadByte(read);
        reader.ReadCount.Should().Be(utf8Size);
        return utf8Bytes.AsSpan().SequenceEqual(read);
    }

    [PropertyTest]
    public bool SpanOfUnmanagedStruct(SimpleStructData[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer, out var reader);
        writer.WriteStruct(in value);
        writer.WrittenCount.Should().Be(size);

        Span<SimpleStructData> read = stackalloc SimpleStructData[value.Length];
        reader.ReadStruct(in read);

        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }

    static int writeOffset;
    static int readOffset;

    static int Setup<T>(
        IReadOnlyCollection<T> values,
        Endianness endianness,
        out BinaryRawBufferWriter writer,
        out BinaryBufferReader reader) where T : struct
    {
        var size = Unsafe.SizeOf<T>() * values.Count;
        Span<byte> buffer = new byte[size];
        writeOffset = 0;
        readOffset = 0;
        writer = new(buffer, ref writeOffset, endianness);
        reader = new(buffer, ref readOffset, endianness);
        return size;
    }
}
