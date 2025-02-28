using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using Backdash.Network;
using Backdash.Serialization;
using Backdash.Serialization.Numerics;
using Backdash.Tests.TestUtils;
using Backdash.Tests.TestUtils.Types;

// ReSharper disable CompareOfFloatsByEqualityOperator
#pragma warning disable S1244

namespace Backdash.Tests.Specs.Unit.Serialization;

[Collection(SerialCollectionDefinition.Name)]
public class BinaryBufferReadWriteValueTests
{
    [PropertyTest]
    public bool TestByte(byte value, byte read, Endianness endianness)
    {
        var size = Setup<byte>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestSByte(sbyte value, sbyte read, Endianness endianness)
    {
        var size = Setup<sbyte>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestBool(bool value, bool read, Endianness endianness)
    {
        var size = Setup<bool>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestChar(char value, char read, Endianness endianness)
    {
        var size = Setup<char>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestShort(short value, short read, Endianness endianness)
    {
        var size = Setup<short>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestUShort(ushort value, ushort read, Endianness endianness)
    {
        var size = Setup<ushort>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestInt(int value, int read, Endianness endianness)
    {
        var size = Setup<int>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestUInt(uint value, uint read, Endianness endianness)
    {
        var size = Setup<uint>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestLong(long value, long read, Endianness endianness)
    {
        var size = Setup<long>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestULong(ulong value, ulong read, Endianness endianness)
    {
        var size = Setup<ulong>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestInt128(Int128 value, Int128 read, Endianness endianness)
    {
        var size = Setup<Int128>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestIntU128(UInt128 value, UInt128 read, Endianness endianness)
    {
        var size = Setup<UInt128>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestHalf(Half value, Half read, Endianness endianness)
    {
        var size = Setup<Half>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestFloat(float value, float read, Endianness endianness)
    {
        var size = Setup<float>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestDouble(double value, double read, Endianness endianness)
    {
        var size = Setup<double>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestVector2(Vector2 value, Endianness endianness)
    {
        var size = Setup<Vector2>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadVector2();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestVector2Ref(Vector2 value, Vector2 read, Endianness endianness)
    {
        var size = Setup<Vector2>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestVector3(Vector3 value, Endianness endianness)
    {
        var size = Setup<Vector3>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadVector3();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestVector3Ref(Vector3 value, Vector3 read, Endianness endianness)
    {
        var size = Setup<Vector3>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestVector4(Vector4 value, Endianness endianness)
    {
        var size = Setup<Vector4>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadVector4();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestVector4Ref(Vector4 value, Vector4 read, Endianness endianness)
    {
        var size = Setup<Vector4>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestQuaternion(Quaternion value, Endianness endianness)
    {
        var size = Setup<Quaternion>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadQuaternion();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestQuaternionRef(Quaternion value, Quaternion read, Endianness endianness)
    {
        var size = Setup<Quaternion>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestGuid(Guid value, Guid read, Endianness endianness)
    {
        var size = Setup<Guid>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestTimeSpan(TimeSpan value, TimeSpan read, Endianness endianness)
    {
        var size = Setup<TimeSpan>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestTimeOnly(TimeOnly value, TimeOnly read, Endianness endianness)
    {
        var size = Setup<TimeOnly>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestDateTime(DateTime value, DateTime read, Endianness endianness)
    {
        const int kindSize = 1;
        var size = Setup<DateTime>(endianness, out var writer) + kindSize;
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestDateTimeOffset(DateTimeOffset value, DateTimeOffset read, Endianness endianness)
    {
        var size = Setup<DateTimeOffset>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestDateOnly(DateOnly value, DateOnly read, Endianness endianness)
    {
        var size = Setup<DateOnly>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool UnmanagedStruct(SimpleStructData value, Endianness endianness)
    {
        var size = Setup<SimpleStructData>(endianness, out var writer);
        writer.WriteStruct(in value);

        var reader = GetReader(writer);
        var read = reader.ReadStruct<SimpleStructData>();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool UnmanagedStructRef(SimpleStructData value, SimpleStructData read, Endianness endianness)
    {
        var size = Setup<SimpleStructData>(endianness, out var writer);
        writer.WriteStruct(in value);

        var reader = GetReader(writer);
        reader.ReadStruct(ref read);

        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SerializableObject(SimpleStructData value, SimpleStructData result, Endianness endianness)
    {
        var size = Setup<SimpleStructData>(endianness, out var writer);

        writer.Write(in value);
        writer.WrittenCount.Should().Be(size);

        var reader = GetReader(writer);
        reader.Read(ref result);
        reader.ReadCount.Should().Be(size);

        return value == result;
    }

    [Collection(SerialCollectionDefinition.Name)]
    public class BinaryIntegerTests
    {
        [PropertyTest] public bool TestByte(byte value, Endianness endianness) => TestInteger(value, endianness);

        [PropertyTest]
        public bool TestSByte(sbyte value, Endianness endianness) =>
            TestInteger(value, endianness);

        [PropertyTest]
        public bool TestShort(short value, Endianness endianness) =>
            TestInteger(value, endianness);

        [PropertyTest]
        public bool TestUShort(ushort value, Endianness endianness) =>
            TestInteger(value, endianness);

        [PropertyTest] public bool TestInt(int value, Endianness endianness) => TestInteger(value, endianness);
        [PropertyTest] public bool TestUInt(uint value, Endianness endianness) => TestInteger(value, endianness);

        [PropertyTest] public bool TestLong(long value, Endianness endianness) => TestInteger(value, endianness);

        [PropertyTest]
        public bool TestULong(ulong value, Endianness endianness) =>
            TestInteger(value, endianness);

        [PropertyTest] public bool TestInt128(Int128 value, Endianness endianness) => TestInteger(value, endianness);

        [PropertyTest]
        public bool TestUInt128(UInt128 value, Endianness endianness) => TestInteger(value, endianness);

        static bool TestInteger<T>(T value, Endianness endianness)
            where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
        {
            var size = Setup<T>(endianness, out var writer);
            writer.WriteNumber(value);
            var reader = GetReader(writer);
            T read = default;
            reader.ReadNumber(ref read);
            reader.ReadCount.Should().Be(size);
            return EqualityComparer<T>.Default.Equals(read, value);
        }
    }

    [Collection(SerialCollectionDefinition.Name)]
    public class CastingAsTests
    {
        [PropertyTest]
        public bool TestByte(ByteEnum value, ByteEnum read, Endianness endianness)
        {
            var size = Setup<ByteEnum>(endianness, out var writer);
            writer.WriteAsByte(in value);

            var reader = GetReader(writer);
            reader.ReadAsByte(ref read);
            reader.ReadCount.Should().Be(size);

            ResetRead();
            var otherRead = reader.ReadAsByte<ByteEnum>();
            reader.ReadCount.Should().Be(size);
            otherRead.Should().Be(read);

            return value == read;
        }

        [PropertyTest]
        public bool TestSByte(SByteEnum value, SByteEnum read, Endianness endianness)
        {
            var size = Setup<SByteEnum>(endianness, out var writer);
            writer.WriteAsSByte(in value);

            var reader = GetReader(writer);
            reader.ReadAsSByte(ref read);
            reader.ReadCount.Should().Be(size);

            ResetRead();
            var otherRead = reader.ReadAsSByte<SByteEnum>();
            reader.ReadCount.Should().Be(size);
            otherRead.Should().Be(read);

            return value == read;
        }

        [PropertyTest]
        public bool TestInt16(Int16Enum value, Int16Enum read, Endianness endianness)
        {
            var size = Setup<Int16Enum>(endianness, out var writer);
            writer.WriteAsInt16(in value);

            var reader = GetReader(writer);
            reader.ReadAsInt16(ref read);
            reader.ReadCount.Should().Be(size);

            ResetRead();
            var otherRead = reader.ReadAsInt16<Int16Enum>();
            reader.ReadCount.Should().Be(size);
            otherRead.Should().Be(read);

            return value == read;
        }

        [PropertyTest]
        public bool TestUInt16(UInt16Enum value, UInt16Enum read, Endianness endianness)
        {
            var size = Setup<UInt16Enum>(endianness, out var writer);
            writer.WriteAsUInt16(in value);

            var reader = GetReader(writer);
            reader.ReadAsUInt16(ref read);
            reader.ReadCount.Should().Be(size);

            ResetRead();
            var otherRead = reader.ReadAsUInt16<UInt16Enum>();
            reader.ReadCount.Should().Be(size);
            otherRead.Should().Be(read);

            return value == read;
        }

        [PropertyTest]
        public bool TestInt32(Int32Enum value, Int32Enum read, Endianness endianness)
        {
            var size = Setup<Int32Enum>(endianness, out var writer);
            writer.WriteAsInt32(in value);

            var reader = GetReader(writer);
            reader.ReadAsInt32(ref read);
            reader.ReadCount.Should().Be(size);

            ResetRead();
            var otherRead = reader.ReadAsInt32<Int32Enum>();
            reader.ReadCount.Should().Be(size);
            otherRead.Should().Be(read);

            return value == read;
        }

        [PropertyTest]
        public bool TestUInt32(UInt32Enum value, UInt32Enum read, Endianness endianness)
        {
            var size = Setup<UInt32Enum>(endianness, out var writer);
            writer.WriteAsUInt32(in value);

            var reader = GetReader(writer);
            reader.ReadAsUInt32(ref read);
            reader.ReadCount.Should().Be(size);

            ResetRead();
            var otherRead = reader.ReadAsUInt32<UInt32Enum>();
            reader.ReadCount.Should().Be(size);
            otherRead.Should().Be(read);

            return value == read;
        }

        [PropertyTest]
        public bool TestInt64(Int64Enum value, Int64Enum read, Endianness endianness)
        {
            var size = Setup<Int64Enum>(endianness, out var writer);
            writer.WriteAsInt64(in value);

            var reader = GetReader(writer);
            reader.ReadAsInt64(ref read);
            reader.ReadCount.Should().Be(size);

            ResetRead();
            var otherRead = reader.ReadAsInt64<Int64Enum>();
            reader.ReadCount.Should().Be(size);
            otherRead.Should().Be(read);

            return value == read;
        }

        [PropertyTest]
        public bool TestUInt64(UInt64Enum value, UInt64Enum read, Endianness endianness)
        {
            var size = Setup<UInt64Enum>(endianness, out var writer);
            writer.WriteAsUInt64(in value);

            var reader = GetReader(writer);
            reader.ReadAsUInt64(ref read);
            reader.ReadCount.Should().Be(size);

            ResetRead();
            var otherRead = reader.ReadAsUInt64<UInt64Enum>();
            reader.ReadCount.Should().Be(size);
            otherRead.Should().Be(read);

            return value == read;
        }
    }

    static int readOffset;

    public static void ResetRead() => readOffset = 0;

    public static int Setup<T>(Endianness endianness, out BinaryBufferWriter writer) where T : unmanaged
    {
        var size = Unsafe.SizeOf<T>();
        readOffset = 0;

        ArrayBufferWriter<byte> buffer = new(size);
        writer = new(buffer, endianness);

        return size;
    }

    public static BinaryBufferReader GetReader(in BinaryBufferWriter writer)
    {
        var buffer = (ArrayBufferWriter<byte>)writer.Buffer;
        return new(buffer.WrittenSpan, ref readOffset, writer.Endianness);
    }
}
