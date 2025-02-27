using System.Buffers;
using System.Runtime.CompilerServices;
using Backdash.Network;
using Backdash.Serialization;
using Backdash.Tests.TestUtils;
using Backdash.Tests.TestUtils.Types;

namespace Backdash.Tests.Specs.Unit.Serialization;

public class BinaryBufferReadWriteListTests
{
    [PropertyTest]
    public bool ListOfByte(List<byte> value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        List<byte> read = [];
        reader.Read(read);
        reader.ReadCount.Should().Be(size);
        return value.SequenceEqual(read);
    }

    [PropertyTest]
    public bool ListOfSByte(List<sbyte> value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        List<sbyte> read = [];
        reader.Read(read);
        reader.ReadCount.Should().Be(size);
        return value.SequenceEqual(read);
    }

    [PropertyTest]
    public bool ListOfShort(List<short> value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        List<short> read = [];
        reader.Read(read);
        reader.ReadCount.Should().Be(size);
        return value.SequenceEqual(read);
    }

    [PropertyTest]
    public bool ListOfUShort(List<ushort> value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        List<ushort> read = [];
        reader.Read(read);
        reader.ReadCount.Should().Be(size);
        return value.SequenceEqual(read);
    }

    [PropertyTest]
    public bool ListOfInt(List<int> value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        List<int> read = [];
        reader.Read(read);
        reader.ReadCount.Should().Be(size);
        return value.SequenceEqual(read);
    }

    [PropertyTest]
    public bool ListOfUInt(List<uint> value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        List<uint> read = [];
        reader.Read(read);
        reader.ReadCount.Should().Be(size);
        return value.SequenceEqual(read);
    }

    [PropertyTest]
    public bool ListOfLong(List<long> value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        List<long> read = [];
        reader.Read(read);
        reader.ReadCount.Should().Be(size);
        return value.SequenceEqual(read);
    }

    [PropertyTest]
    public bool ListOfULong(List<ulong> value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        List<ulong> read = [];
        reader.Read(read);
        reader.ReadCount.Should().Be(size);
        return value.SequenceEqual(read);
    }

    [PropertyTest]
    public bool ListOfInt128(List<Int128> value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        List<Int128> read = [];
        reader.Read(read);
        reader.ReadCount.Should().Be(size);
        return value.SequenceEqual(read);
    }

    [PropertyTest]
    public bool ListOfUInt128(List<UInt128> value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        List<UInt128> read = [];
        reader.Read(read);
        reader.ReadCount.Should().Be(size);
        return value.SequenceEqual(read);
    }

    [PropertyTest]
    public bool ListOfChars(List<char> value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        List<char> read = [];
        reader.Read(read);
        reader.ReadCount.Should().Be(size);
        return value.SequenceEqual(read);
    }

    [PropertyTest]
    public bool ListOfBooleans(List<bool> value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        List<bool> read = [];
        reader.Read(read);
        reader.ReadCount.Should().Be(size);
        return value.SequenceEqual(read);
    }

    [PropertyTest]
    public bool ListOfUtf8Char(NonEmptyString input, Endianness endianness)
    {
        var utf8Bytes = System.Text.Encoding.UTF8.GetBytes(input.Item);
        var value = input.Item.ToList();

        var size = Setup(utf8Bytes, endianness, out var writer);
        writer.WriteUtf8String(in value);
        var reader = GetReader(writer);

        List<char> read = [];
        reader.ReadUtf8String(in read);

        reader.ReadCount.Should().Be(size);
        return value.SequenceEqual(read);
    }

    [PropertyTest]
    public bool ListOfGuids(List<Guid> value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        List<Guid> read = [];
        reader.Read(in read);
        reader.ReadCount.Should().Be(size);
        return value.SequenceEqual(read);
    }

    [PropertyTest]
    public bool ListOfTimeSpans(List<TimeSpan> value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        List<TimeSpan> read = [];
        reader.Read(in read);
        reader.ReadCount.Should().Be(size);
        return value.SequenceEqual(read);
    }

    [PropertyTest]
    public bool ListOfTimeOnly(List<TimeOnly> value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        List<TimeOnly> read = [];
        reader.Read(in read);
        reader.ReadCount.Should().Be(size);
        return value.SequenceEqual(read);
    }

    [PropertyTest]
    public bool ListOfDateOnly(List<DateOnly> value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        List<DateOnly> read = [];
        reader.Read(in read);
        reader.ReadCount.Should().Be(size);
        return value.SequenceEqual(read);
    }

    [PropertyTest]
    public bool ListOfDateTime(List<DateTime> value, Endianness endianness)
    {
        var kindSize = 1 * value.Count;
        var size = Setup(value, endianness, out var writer) + kindSize;
        writer.Write(value);
        var reader = GetReader(writer);
        List<DateTime> read = [];
        reader.Read(in read);
        reader.ReadCount.Should().Be(size);
        return value.SequenceEqual(read);
    }

    [PropertyTest]
    public bool ListOfDateTimeOffset(List<DateTimeOffset> value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        List<DateTimeOffset> read = [];
        reader.Read(in read);
        reader.ReadCount.Should().Be(size);
        return value.SequenceEqual(read);
    }

    [PropertyTest]
    public bool ListOfUnmanagedStruct(List<SimpleStructData> value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.WriteStruct(in value);
        var reader = GetReader(writer);

        List<SimpleStructData> read = [];
        reader.ReadStruct(in read);

        reader.ReadCount.Should().Be(size);
        return value.SequenceEqual(read);
    }


    [PropertyTest]
    public bool ListOfSerializableObjects(List<SimpleStructData> value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(in value);
        writer.WrittenCount.Should().Be(size);

        var reader = GetReader(writer);
        List<SimpleStructData> read = [];
        reader.Read(in read);
        reader.ReadCount.Should().Be(size);

        return value.SequenceEqual(read);
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
        return size + sizeof(int);
    }

    static BinaryBufferReader GetReader(in BinaryBufferWriter writer)
    {
        var buffer = (ArrayBufferWriter<byte>)writer.Buffer;
        return new(buffer.WrittenSpan, ref readOffset, writer.Endianness);
    }
}
