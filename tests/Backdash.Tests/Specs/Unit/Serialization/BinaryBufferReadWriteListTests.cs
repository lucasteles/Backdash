using System.Buffers;
using System.Runtime.CompilerServices;
using Backdash.Data;
using Backdash.Network;
using Backdash.Serialization;
using Backdash.Tests.TestUtils;
using Backdash.Tests.TestUtils.Types;
using FsCheck.Fluent;

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
    public bool ListOfSerializableStruct(List<SimpleStructData> value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(in value);
        writer.WrittenCount.Should().Be(size);

        var reader = GetReader(writer);
        List<SimpleStructData> read = [];
        reader.Read(read);
        reader.ReadCount.Should().Be(size);

        return value.SequenceEqual(read);
    }

    [PropertyTest]
    public bool ListOfSerializableClass(List<SimpleRefData> value, Endianness endianness)
    {
        Setup(endianness, out var writer);
        writer.Write(in value);

        var reader = GetReader(writer);
        List<SimpleRefData> read = [];
        reader.Read(read);

        return value.SequenceEqual(read);
    }

    [Collection(SerialCollectionDefinition.Name)]
    public class CastingAsTests
    {
        [PropertyTest]
        public bool ListOfByte(List<ByteEnum> value, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.WriteAsByte(value);
            var reader = GetReader(writer);
            List<ByteEnum> read = [];
            reader.ReadAsByte(read);
            reader.ReadCount.Should().Be(size);
            return value.SequenceEqual(read);
        }

        [PropertyTest]
        public bool ListOfSByte(List<SByteEnum> value, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.WriteAsSByte(value);
            var reader = GetReader(writer);
            List<SByteEnum> read = [];
            reader.ReadAsSByte(read);
            reader.ReadCount.Should().Be(size);
            return value.SequenceEqual(read);
        }

        [PropertyTest]
        public bool ListOfInt16(List<Int16Enum> value, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.WriteAsInt16(value);
            var reader = GetReader(writer);
            List<Int16Enum> read = [];
            reader.ReadAsInt16(read);
            reader.ReadCount.Should().Be(size);
            return value.SequenceEqual(read);
        }

        [PropertyTest]
        public bool ListOfUInt16(List<UInt16Enum> value, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.WriteAsUInt16(value);
            var reader = GetReader(writer);
            List<UInt16Enum> read = [];
            reader.ReadAsUInt16(read);
            reader.ReadCount.Should().Be(size);
            return value.SequenceEqual(read);
        }

        [PropertyTest]
        public bool ListOfInt32(List<Int32Enum> value, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.WriteAsInt32(value);
            var reader = GetReader(writer);
            List<Int32Enum> read = [];
            reader.ReadAsInt32(read);
            reader.ReadCount.Should().Be(size);
            return value.SequenceEqual(read);
        }

        [PropertyTest]
        public bool ListOfUInt32(List<UInt32Enum> value, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.WriteAsUInt32(value);
            var reader = GetReader(writer);
            List<UInt32Enum> read = [];
            reader.ReadAsUInt32(read);
            reader.ReadCount.Should().Be(size);
            return value.SequenceEqual(read);
        }

        [PropertyTest]
        public bool ListOfInt64(List<Int64Enum> value, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.WriteAsInt64(value);
            var reader = GetReader(writer);
            List<Int64Enum> read = [];
            reader.ReadAsInt64(read);
            reader.ReadCount.Should().Be(size);
            return value.SequenceEqual(read);
        }

        [PropertyTest]
        public bool ListOfUInt64(List<UInt64Enum> value, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.WriteAsUInt64(value);
            var reader = GetReader(writer);
            List<UInt64Enum> read = [];
            reader.ReadAsUInt64(read);
            reader.ReadCount.Should().Be(size);
            return value.SequenceEqual(read);
        }
    }

    [Fact]
    public void ShouldRentClassFromPoolWhenExpandEmptyList()
    {
        TestObjectPool<SimpleRefData> pool = new();
        var values = TestGenerators.For<SimpleRefData>().Sample(10).ToList();

        Setup(Endianness.LittleEndian, out var writer);
        writer.Write(in values);

        var reader = GetReader(writer);
        List<SimpleRefData> copy = [];
        reader.Read(copy, pool);

        pool.RentCount.Should().Be(10);
        pool.ReturnCount.Should().Be(0);
        copy.Should().Equal(values);
    }

    [Fact]
    public void ShouldRentedClassFromPoolWhenExpandInitializedList()
    {
        TestObjectPool<SimpleRefData> pool = new();
        var values = TestGenerators.For<SimpleRefData>().Sample(10).ToList();

        Setup(Endianness.LittleEndian, out var writer);
        writer.Write(in values);

        var reader = GetReader(writer);
        var copy = TestGenerators.For<SimpleRefData>().Sample(5).ToList();
        reader.Read(copy, pool);

        copy.Should().Equal(values);
        pool.RentCount.Should().Be(5);
        pool.ReturnCount.Should().Be(0);
    }

    [Fact]
    public void ShouldReturnRentedClassFromPoolWhenExpandInitializedList()
    {
        TestObjectPool<SimpleRefData> pool = new();
        var target = TestGenerators.For<SimpleRefData>().Sample(5).ToList();

        Setup(Endianness.LittleEndian, out var writer);
        writer.Write(in target);

        var reader = GetReader(writer);
        var source1 = TestGenerators.For<SimpleRefData>().Sample(5).ToList();
        var source2 = TestGenerators.For<SimpleRefData>().Sample(5).ToList();
        List<SimpleRefData> source = [.. source1, .. source2];

        reader.Read(source, pool);

        source1.Should().Equal(target);
        pool.RentCount.Should().Be(0);
        pool.ReturnCount.Should().Be(5);
        pool.Returned.Should().Equal(source2);
    }

    [PropertyTest]
    public bool CircularBufferOfSerializableObjects(CircularBuffer<SimpleStructData> value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(in value);
        writer.WrittenCount.Should().Be(size);

        var reader = GetReader(writer);
        CircularBuffer<SimpleStructData> read = new(value.Size);
        reader.Read(in read);
        reader.ReadCount.Should().Be(size);

        return value == read;
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

    static void Setup(Endianness endianness, out BinaryBufferWriter writer)
    {
        readOffset = 0;
        ArrayBufferWriter<byte> buffer = new();
        writer = new(buffer, endianness);
    }

    static BinaryBufferReader GetReader(in BinaryBufferWriter writer)
    {
        var buffer = (ArrayBufferWriter<byte>)writer.Buffer;
        return new(buffer.WrittenSpan, ref readOffset, writer.Endianness);
    }

    class TestObjectPool<T> : IObjectPool<T> where T : new()
    {
        readonly List<T> returned = [];

        public int RentCount { get; private set; }
        public int ReturnCount => returned.Count;
        public IReadOnlyList<T> Returned => returned.AsReadOnly();

        public T Rent()
        {
            RentCount++;
            return new();
        }

        public void Return(T value) => returned.Add(value);
    }
}
