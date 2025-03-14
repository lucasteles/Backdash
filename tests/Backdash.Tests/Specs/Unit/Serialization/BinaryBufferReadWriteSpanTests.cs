using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
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
        reader.Read(read);
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
        reader.Read(read);
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
        reader.Read(read);
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
        reader.Read(read);
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
        reader.Read(read);
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
        reader.Read(read);
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
        reader.Read(read);
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
        reader.Read(read);
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
        reader.Read(read);
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
        reader.Read(read);
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
        reader.Read(read);
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
        reader.Read(read);
        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }

    [PropertyTest]
    public bool SpanOfFloat(float[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        Span<float> read = stackalloc float[value.Length];
        reader.Read(read);
        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }

    [PropertyTest]
    public bool SpanOfDouble(double[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        Span<double> read = stackalloc double[value.Length];
        reader.Read(read);
        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }

    [PropertyTest]
    public bool SpanOfHalf(Half[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        Span<Half> read = stackalloc Half[value.Length];
        reader.Read(read);
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
        reader.Read(in read);
        reader.ReadCount.Should().Be(utf8Size);
        return utf8Bytes.AsSpan().SequenceEqual(read);
    }

    [PropertyTest]
    public bool String(NonEmptyString input, PositiveInt stringSize)
    {
        var value = input.Item;
        var size = stringSize.Item;

        Setup(Endianness.LittleEndian, out var writer);
        writer.WriteString(value, size);
        var reader = GetReader(writer);

        var result = reader.ReadString(size);

        if (value.Length > size)
            value = value[..size];

        return string.Equals(value.Trim(), result.Trim());
    }

    [PropertyTest]
    public bool StringBuilder(StringBuilder value, StringBuilder read)
    {
        Setup(Endianness.LittleEndian, out var writer);
        writer.Write(in value);

        var reader = GetReader(writer);
        reader.Read(in read);

        return string.Equals(value.ToString(), read.ToString());
    }

    [PropertyTest]
    public bool SpanOfGuids(Guid[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        Span<Guid> read = stackalloc Guid[value.Length];
        reader.Read(in read);
        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }

    [PropertyTest]
    public bool SpanOfTimeSpans(TimeSpan[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        Span<TimeSpan> read = stackalloc TimeSpan[value.Length];
        reader.Read(in read);
        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }

    [PropertyTest]
    public bool SpanOfTimeOnly(TimeOnly[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        Span<TimeOnly> read = stackalloc TimeOnly[value.Length];
        reader.Read(in read);
        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }

    [PropertyTest]
    public bool SpanOfDateOnly(DateOnly[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        Span<DateOnly> read = stackalloc DateOnly[value.Length];
        reader.Read(in read);
        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }

    [PropertyTest]
    public bool SpanOfFrame(Frame[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        Span<Frame> read = stackalloc Frame[value.Length];
        reader.Read(in read);
        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }

    [PropertyTest]
    public bool SpanOfDateTime(DateTime[] value, Endianness endianness)
    {
        var kindSize = 1 * value.Length;
        var size = Setup(value, endianness, out var writer) + kindSize;
        writer.Write(value);
        var reader = GetReader(writer);
        Span<DateTime> read = stackalloc DateTime[value.Length];
        reader.Read(in read);
        reader.ReadCount.Should().Be(size);
        return value.AsSpan().SequenceEqual(read);
    }

    [PropertyTest]
    public bool SpanOfDateTimeOffset(DateTimeOffset[] value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        Span<DateTimeOffset> read = stackalloc DateTimeOffset[value.Length];
        reader.Read(in read);
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

    [Collection(SerialCollectionDefinition.Name)]
    public class CastingAsTests
    {
        [PropertyTest]
        public bool SpanOfByte(ByteEnum[] value, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.WriteAsByte(value);
            var reader = GetReader(writer);
            Span<ByteEnum> read = stackalloc ByteEnum[value.Length];
            reader.ReadAsByte(read);
            reader.ReadCount.Should().Be(size);
            return value.AsSpan().SequenceEqual(read);
        }

        [PropertyTest]
        public bool SpanOfSByte(SByteEnum[] value, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.WriteAsSByte(value);
            var reader = GetReader(writer);
            Span<SByteEnum> read = stackalloc SByteEnum[value.Length];
            reader.ReadAsSByte(read);
            reader.ReadCount.Should().Be(size);
            return value.AsSpan().SequenceEqual(read);
        }

        [PropertyTest]
        public bool SpanOfInt16(Int16Enum[] value, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.WriteAsInt16(value);
            var reader = GetReader(writer);
            Span<Int16Enum> read = stackalloc Int16Enum[value.Length];
            reader.ReadAsInt16(read);
            reader.ReadCount.Should().Be(size);
            return value.AsSpan().SequenceEqual(read);
        }

        [PropertyTest]
        public bool SpanOfUInt16(UInt16Enum[] value, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.WriteAsUInt16(value);
            var reader = GetReader(writer);
            Span<UInt16Enum> read = stackalloc UInt16Enum[value.Length];
            reader.ReadAsUInt16(read);
            reader.ReadCount.Should().Be(size);
            return value.AsSpan().SequenceEqual(read);
        }

        [PropertyTest]
        public bool SpanOfInt32(Int32Enum[] value, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.WriteAsInt32(value);
            var reader = GetReader(writer);
            Span<Int32Enum> read = stackalloc Int32Enum[value.Length];
            reader.ReadAsInt32(read);
            reader.ReadCount.Should().Be(size);
            return value.AsSpan().SequenceEqual(read);
        }

        [PropertyTest]
        public bool SpanOfUInt32(UInt32Enum[] value, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.WriteAsUInt32(value);
            var reader = GetReader(writer);
            Span<UInt32Enum> read = stackalloc UInt32Enum[value.Length];
            reader.ReadAsUInt32(read);
            reader.ReadCount.Should().Be(size);
            return value.AsSpan().SequenceEqual(read);
        }

        [PropertyTest]
        public bool SpanOfInt64(Int64Enum[] value, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.WriteAsInt64(value);
            var reader = GetReader(writer);
            Span<Int64Enum> read = stackalloc Int64Enum[value.Length];
            reader.ReadAsInt64(read);
            reader.ReadCount.Should().Be(size);
            return value.AsSpan().SequenceEqual(read);
        }

        [PropertyTest]
        public bool SpanOfUInt64(UInt64Enum[] value, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.WriteAsUInt64(value);
            var reader = GetReader(writer);
            Span<UInt64Enum> read = stackalloc UInt64Enum[value.Length];
            reader.ReadAsUInt64(read);
            reader.ReadCount.Should().Be(size);
            return value.AsSpan().SequenceEqual(read);
        }
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

    static void Setup(
        Endianness endianness,
        out BinaryBufferWriter writer
    )
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
}
