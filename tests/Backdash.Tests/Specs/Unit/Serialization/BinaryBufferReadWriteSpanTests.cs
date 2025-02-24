using System.Buffers;
using System.Runtime.CompilerServices;
using Backdash.Network;
using Backdash.Serialization;
using Backdash.Tests.TestUtils;
using Backdash.Tests.TestUtils.Types;

namespace Backdash.Tests.Specs.Unit.Serialization;

public class BinaryBufferReadWriteSpanTests
{
    [PropertyTest]
    public bool SpanOfByte(byte[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        Span<byte> read = stackalloc byte[value.Length];
        reader.ReadByte(read);
        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }

    [PropertyTest]
    public bool SpanOfSByte(sbyte[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        Span<sbyte> read = stackalloc sbyte[value.Length];
        reader.ReadSByte(read);
        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }

    [PropertyTest]
    public bool SpanOfShort(short[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        Span<short> read = stackalloc short[value.Length];
        reader.ReadInt16(read);
        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }

    [PropertyTest]
    public bool SpanOfUShort(ushort[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        Span<ushort> read = stackalloc ushort[value.Length];
        reader.ReadUInt16(read);
        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }

    [PropertyTest]
    public bool SpanOfInt(int[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        Span<int> read = stackalloc int[value.Length];
        reader.ReadInt32(read);
        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }

    [PropertyTest]
    public bool SpanOfUInt(uint[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        Span<uint> read = stackalloc uint[value.Length];
        reader.ReadUInt32(read);
        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }

    [PropertyTest]
    public bool SpanOfLong(long[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        Span<long> read = stackalloc long[value.Length];
        reader.ReadInt64(read);
        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }

    [PropertyTest]
    public bool SpanOfULong(ulong[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        Span<ulong> read = stackalloc ulong[value.Length];
        reader.ReadUInt64(read);
        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }

    [PropertyTest]
    public bool SpanOfInt128(Int128[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        Span<Int128> read = stackalloc Int128[value.Length];
        reader.ReadInt128(read);
        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }

    [PropertyTest]
    public bool SpanOfUInt128(UInt128[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        Span<UInt128> read = stackalloc UInt128[value.Length];
        reader.ReadUInt128(read);
        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }

    [PropertyTest]
    public bool SpanOfChars(char[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        Span<char> read = stackalloc char[value.Length];
        reader.ReadChar(read);
        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }

    [PropertyTest]
    public bool SpanOfBooleans(bool[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
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

        var size = Setup(utf8Bytes, endianness, out var writer);
        writer.WriteUtf8String(value);
        var reader = GetReader(writer);

        Span<char> read = stackalloc char[value.Length];
        reader.ReadUtf8String(in read);

        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }

    [PropertyTest]
    public bool SpanOfUtf8Bytes(NonEmptyString input, Endianness endianness)
    {
        var value = input.Item;
        var utf8Size = System.Text.Encoding.UTF8.GetByteCount(value);
        var utf8Bytes = System.Text.Encoding.UTF8.GetBytes(value);

        var size = Setup(utf8Bytes, endianness, out var writer);
        size.Should().Be(utf8Size);
        writer.WriteUtf8String(value);
        var reader = GetReader(writer);

        Span<byte> read = stackalloc byte[utf8Size];
        reader.ReadByte(in read);
        reader.ReadCount.Should().Be(utf8Size);
        return utf8Bytes.AsSpan().SequenceEqual(read);
    }

    [PropertyTest]
    public bool String(NonEmptyString input, Endianness endianness)
    {
        var value = input.Item;

        var size = Setup(value.ToCharArray(), endianness, out var writer);
        writer.WriteString(value);
        var reader = GetReader(writer);

        var result = reader.ReadString(value.Length);

        reader.ReadCount.Should().Be(size);
        return string.Equals(value, result);
    }

    [PropertyTest]
    public bool SpanOfGuids(Guid[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        Span<Guid> read = stackalloc Guid[value.Length];
        reader.ReadGuid(in read);
        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }

    [PropertyTest]
    public bool SpanOfUnmanagedStruct(SimpleStructData[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.WriteStruct(in value);
        var reader = GetReader(writer);

        Span<SimpleStructData> read = stackalloc SimpleStructData[value.Length];
        reader.ReadStruct(in read);

        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }

    [PropertyTest]
    public bool SpanOfSerializableObjects(SimpleStructData[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(in value);
        writer.WrittenCount.Should().Be(size);

        var reader = GetReader(writer);
        var read = new SimpleStructData[value.Length];
        reader.Read(in read);
        reader.ReadCount.Should().Be(size);

        return value.AsSpan().SequenceEqual(read);
    }

    static int readOffset;

    static int Setup<T>(
        IReadOnlyCollection<T> values,
        Endianness endianness,
        out BinaryBufferWriter writer
    )
        where T : struct
    {
        var size = Unsafe.SizeOf<T>() * values.Count;
        readOffset = 0;
        ArrayBufferWriter<byte> buffer = new(size is 0 ? 1 : size);
        writer = new(buffer, endianness);
        return size;
    }

    static BinaryBufferReader GetReader(in BinaryBufferWriter writer)
    {
        var buffer = (ArrayBufferWriter<byte>)writer.Buffer;
        return new(buffer.WrittenSpan, ref readOffset, writer.Endianness);
    }
}
