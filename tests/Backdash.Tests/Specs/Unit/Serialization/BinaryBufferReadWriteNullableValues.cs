using System.Numerics;
using Backdash.Network;
using Backdash.Serialization;
using Backdash.Serialization.Numerics;
using Backdash.Tests.TestUtils;
using Backdash.Tests.TestUtils.Types;

// ReSharper disable CompareOfFloatsByEqualityOperator
#pragma warning disable S1244

namespace Backdash.Tests.Specs.Unit.Serialization;

[Collection(SerialCollectionDefinition.Name)]
public class BinaryBufferReadWriteNullableValues
{
    [PropertyTest]
    public bool TestByte(byte? value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);

        writer.Write(in value);
        var reader = GetReader(writer);
        var read = reader.ReadNullableByte();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestSByte(sbyte? value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadNullableSByte();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestBool(bool? value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadNullableBoolean();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestChar(char? value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadNullableChar();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestShort(short? value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadNullableInt16();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestUShort(ushort? value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadNullableUInt16();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestInt(int? value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadNullableInt32();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestUInt(uint? value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadNullableUInt32();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestLong(long? value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadNullableInt64();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestULong(ulong? value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadNullableUInt64();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestInt128(Int128? value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadNullableInt128();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestIntU128(UInt128? value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadNullableUInt128();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestHalf(Half? value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadNullableHalf();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestFloat(float? value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadNullableFloat();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestDouble(double? value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadNullableDouble();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestVector2(Vector2? value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadNullableVector2();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestVector3(Vector3? value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadNullableVector3();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestVector4(Vector4? value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadNullableVector4();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestQuaternion(Quaternion? value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadNullableQuaternion();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestGuid(Guid? value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadNullableGuid();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestTimeSpan(TimeSpan? value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadNullableTimeSpan();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestTimeOnly(TimeOnly? value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadNullableTimeOnly();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestDateTime(DateTime? value, Endianness endianness)
    {
        const int kindSize = 1;
        var size = Setup(value, endianness, out var writer);

        if (value.HasValue)
            size += kindSize;

        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadNullableDateTime();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestDateTimeOffset(DateTimeOffset? value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadNullableDateTimeOffset();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool TestDateOnly(DateOnly? value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadNullableDateOnly();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool UnmanagedStruct(SimpleStructData? value, Endianness endianness)
    {
        var size = Setup(value, endianness, out var writer);
        writer.WriteStruct(in value);

        var reader = GetReader(writer);
        var read = reader.ReadNullableStruct<SimpleStructData>();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [Collection(SerialCollectionDefinition.Name)]
    public class BinaryIntegerTests
    {
        [PropertyTest] public bool TestByte(byte? value, Endianness endianness) => TestInteger(value, endianness);

        [PropertyTest]
        public bool TestSByte(sbyte? value, Endianness endianness) =>
            TestInteger(value, endianness);

        [PropertyTest]
        public bool TestShort(short? value, Endianness endianness) =>
            TestInteger(value, endianness);

        [PropertyTest]
        public bool TestUShort(ushort? value, Endianness endianness) =>
            TestInteger(value, endianness);

        [PropertyTest] public bool TestInt(int? value, Endianness endianness) => TestInteger(value, endianness);
        [PropertyTest] public bool TestUInt(uint? value, Endianness endianness) => TestInteger(value, endianness);

        [PropertyTest] public bool TestLong(long? value, Endianness endianness) => TestInteger(value, endianness);

        [PropertyTest]
        public bool TestULong(ulong? value, Endianness endianness) =>
            TestInteger(value, endianness);

        [PropertyTest] public bool TestInt128(Int128? value, Endianness endianness) => TestInteger(value, endianness);

        [PropertyTest]
        public bool TestUInt128(UInt128? value, Endianness endianness) => TestInteger(value, endianness);

        static bool TestInteger<T>(T? value, Endianness endianness)
            where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
        {
            var size = Setup(value, endianness, out var writer);
            writer.WriteNumber(value);
            var reader = GetReader(writer);
            T? read = null;
            reader.ReadNumber(ref read);
            reader.ReadCount.Should().Be(size);
            return EqualityComparer<T?>.Default.Equals(read, value);
        }
    }

    [Collection(SerialCollectionDefinition.Name)]
    public class NullableRefTests
    {
        [PropertyTest]
        public bool TestByte(byte? value, byte? read, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);

            writer.Write(in value);
            var reader = GetReader(writer);
            reader.Read(ref read);
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool TestSByte(sbyte? value, sbyte? read, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.Write(value);
            var reader = GetReader(writer);
            reader.Read(ref read);
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool TestBool(bool? value, bool? read, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.Write(value);
            var reader = GetReader(writer);
            reader.Read(ref read);
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool TestChar(char? value, char? read, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.Write(value);
            var reader = GetReader(writer);
            reader.Read(ref read);
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool TestShort(short? value, short? read, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.Write(value);
            var reader = GetReader(writer);
            reader.Read(ref read);
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool TestUShort(ushort? value, ushort? read, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.Write(value);
            var reader = GetReader(writer);
            reader.Read(ref read);
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool TestInt(int? value, int? read, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.Write(value);
            var reader = GetReader(writer);
            reader.Read(ref read);
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool TestUInt(uint? value, uint? read, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.Write(value);
            var reader = GetReader(writer);
            reader.Read(ref read);
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool TestLong(long? value, long? read, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.Write(value);
            var reader = GetReader(writer);
            reader.Read(ref read);
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool TestULong(ulong? value, ulong? read, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.Write(value);
            var reader = GetReader(writer);
            reader.Read(ref read);
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool TestInt128(Int128? value, Int128? read, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.Write(value);
            var reader = GetReader(writer);
            reader.Read(ref read);
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool TestIntU128(UInt128? value, UInt128? read, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.Write(value);
            var reader = GetReader(writer);
            reader.Read(ref read);
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool TestHalf(Half? value, Half? read, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.Write(value);
            var reader = GetReader(writer);
            reader.Read(ref read);
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool TestFloat(float? value, float? read, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.Write(value);
            var reader = GetReader(writer);
            reader.Read(ref read);
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool TestDouble(double? value, double? read, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.Write(value);
            var reader = GetReader(writer);
            reader.Read(ref read);
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool TestVector2(Vector2? value, Vector2? read, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.Write(value);
            var reader = GetReader(writer);
            reader.Read(ref read);
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool TestVector3(Vector3? value, Vector3? read, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.Write(value);
            var reader = GetReader(writer);
            reader.Read(ref read);
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool TestVector4(Vector4? value, Vector4? read, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.Write(value);
            var reader = GetReader(writer);
            reader.Read(ref read);
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool TestQuaternion(Quaternion? value, Quaternion? read, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.Write(value);
            var reader = GetReader(writer);
            reader.Read(ref read);
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool TestGuid(Guid? value, Guid? read, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.Write(value);
            var reader = GetReader(writer);
            reader.Read(ref read);
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool TestTimeSpan(TimeSpan? value, TimeSpan? read, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.Write(value);
            var reader = GetReader(writer);
            reader.Read(ref read);
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool TestTimeOnly(TimeOnly? value, TimeOnly? read, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.Write(value);
            var reader = GetReader(writer);
            reader.Read(ref read);
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool TestDateTime(DateTime? value, DateTime? read, Endianness endianness)
        {
            const int kindSize = 1;
            var size = Setup(value, endianness, out var writer);

            if (value.HasValue)
                size += kindSize;

            writer.Write(value);
            var reader = GetReader(writer);
            reader.Read(ref read);
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool TestDateTimeOffset(DateTimeOffset? value, DateTimeOffset? read, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.Write(value);
            var reader = GetReader(writer);
            reader.Read(ref read);
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool TestDateOnly(DateOnly? value, DateOnly? read, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.Write(value);
            var reader = GetReader(writer);
            reader.Read(ref read);
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool UnmanagedStruct(SimpleStructData? value, SimpleStructData? read, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.WriteStruct(in value);

            var reader = GetReader(writer);
            reader.ReadStruct(ref read);
            reader.ReadCount.Should().Be(size);
            return value == read;
        }
    }

    [Collection(SerialCollectionDefinition.Name)]
    public class CastingAsTests
    {
        [PropertyTest]
        public bool TestByte(ByteEnum? value, ByteEnum? read, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.WriteAsByte(in value);

            var reader = GetReader(writer);
            reader.ReadAsByte(ref read);
            reader.ReadCount.Should().Be(size);

            ResetRead();
            var otherRead = reader.ReadAsNullableByte<ByteEnum>();
            reader.ReadCount.Should().Be(size);
            otherRead.Should().Be(read);

            return value == read;
        }

        [PropertyTest]
        public bool TestSByte(SByteEnum? value, SByteEnum? read, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.WriteAsSByte(in value);

            var reader = GetReader(writer);
            reader.ReadAsSByte(ref read);
            reader.ReadCount.Should().Be(size);

            ResetRead();
            var otherRead = reader.ReadAsNullableSByte<SByteEnum>();
            reader.ReadCount.Should().Be(size);
            otherRead.Should().Be(read);

            return value == read;
        }

        [PropertyTest]
        public bool TestInt16(Int16Enum? value, Int16Enum? read, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.WriteAsInt16(in value);

            var reader = GetReader(writer);
            reader.ReadAsInt16(ref read);
            reader.ReadCount.Should().Be(size);

            ResetRead();
            var otherRead = reader.ReadAsNullableInt16<Int16Enum>();
            reader.ReadCount.Should().Be(size);
            otherRead.Should().Be(read);

            return value == read;
        }

        [PropertyTest]
        public bool TestUInt16(UInt16Enum? value, UInt16Enum? read, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.WriteAsUInt16(in value);

            var reader = GetReader(writer);
            reader.ReadAsUInt16(ref read);
            reader.ReadCount.Should().Be(size);

            ResetRead();
            var otherRead = reader.ReadAsNullableUInt16<UInt16Enum>();
            reader.ReadCount.Should().Be(size);
            otherRead.Should().Be(read);

            return value == read;
        }

        [PropertyTest]
        public bool TestInt32(Int32Enum? value, Int32Enum? read, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.WriteAsInt32(in value);

            var reader = GetReader(writer);
            reader.ReadAsInt32(ref read);
            reader.ReadCount.Should().Be(size);

            ResetRead();
            var otherRead = reader.ReadAsNullableInt32<Int32Enum>();
            reader.ReadCount.Should().Be(size);
            otherRead.Should().Be(read);

            return value == read;
        }

        [PropertyTest]
        public bool TestUInt32(UInt32Enum? value, UInt32Enum? read, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.WriteAsUInt32(in value);

            var reader = GetReader(writer);
            reader.ReadAsUInt32(ref read);
            reader.ReadCount.Should().Be(size);

            ResetRead();
            var otherRead = reader.ReadAsNullableUInt32<UInt32Enum>();
            reader.ReadCount.Should().Be(size);
            otherRead.Should().Be(read);

            return value == read;
        }

        [PropertyTest]
        public bool TestInt64(Int64Enum? value, Int64Enum? read, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.WriteAsInt64(in value);

            var reader = GetReader(writer);
            reader.ReadAsInt64(ref read);
            reader.ReadCount.Should().Be(size);

            ResetRead();
            var otherRead = reader.ReadAsNullableInt64<Int64Enum>();
            reader.ReadCount.Should().Be(size);
            otherRead.Should().Be(read);

            return value == read;
        }

        [PropertyTest]
        public bool TestUInt64(UInt64Enum? value, UInt64Enum? read, Endianness endianness)
        {
            var size = Setup(value, endianness, out var writer);
            writer.WriteAsUInt64(in value);

            var reader = GetReader(writer);
            reader.ReadAsUInt64(ref read);
            reader.ReadCount.Should().Be(size);

            ResetRead();
            var otherRead = reader.ReadAsNullableUInt64<UInt64Enum>();
            reader.ReadCount.Should().Be(size);
            otherRead.Should().Be(read);

            return value == read;
        }
    }

    public static int Setup<T>(T? value, Endianness endianness, out BinaryBufferWriter writer) where T : unmanaged
    {
        var size = BinaryBufferReadWriteValueTests.Setup<T>(endianness, out writer);
        return (value.HasValue ? size : 0) + 1;
    }

    public static void ResetRead() => BinaryBufferReadWriteValueTests.ResetRead();

    public static BinaryBufferReader GetReader(in BinaryBufferWriter writer) =>
        BinaryBufferReadWriteValueTests.GetReader(in writer);
}
