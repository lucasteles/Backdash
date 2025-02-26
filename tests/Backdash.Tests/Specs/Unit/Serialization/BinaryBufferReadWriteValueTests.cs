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
    public bool SingleByte(byte value, Endianness endianness)
    {
        var size = Setup<byte>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadByte();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleSByte(sbyte value, Endianness endianness)
    {
        var size = Setup<sbyte>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadSByte();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleBool(bool value, Endianness endianness)
    {
        var size = Setup<bool>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadBoolean();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleChar(char value, Endianness endianness)
    {
        var size = Setup<char>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadChar();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleShort(short value, Endianness endianness)
    {
        var size = Setup<short>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadInt16();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleUShort(ushort value, Endianness endianness)
    {
        var size = Setup<ushort>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadUInt16();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleInt(int value, Endianness endianness)
    {
        var size = Setup<int>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadInt32();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleUInt(uint value, Endianness endianness)
    {
        var size = Setup<uint>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadUInt32();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleLong(long value, Endianness endianness)
    {
        var size = Setup<long>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadInt64();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleULong(ulong value, Endianness endianness)
    {
        var size = Setup<ulong>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadUInt64();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleInt128(Int128 value, Endianness endianness)
    {
        var size = Setup<Int128>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadInt128();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleIntU128(UInt128 value, Endianness endianness)
    {
        var size = Setup<UInt128>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadUInt128();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleHalf(Half value, Endianness endianness)
    {
        var size = Setup<Half>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadHalf();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleFloat(float value, Endianness endianness)
    {
        var size = Setup<float>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadFloat();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleDouble(double value, Endianness endianness)
    {
        var size = Setup<double>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadDouble();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleVector2(Vector2 value, Endianness endianness)
    {
        var size = Setup<Vector2>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadVector2();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleVector2Ref(Vector2 value, Endianness endianness)
    {
        var size = Setup<Vector2>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        Vector2 read = new();
        reader.ReadVector2(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleVector3(Vector3 value, Endianness endianness)
    {
        var size = Setup<Vector3>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadVector3();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleVector3Ref(Vector3 value, Endianness endianness)
    {
        var size = Setup<Vector3>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        Vector3 read = new();
        reader.ReadVector3(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleVector4(Vector4 value, Endianness endianness)
    {
        var size = Setup<Vector4>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadVector4();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleVector4Ref(Vector4 value, Endianness endianness)
    {
        var size = Setup<Vector4>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        Vector4 read = new();
        reader.ReadVector4(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleQuaternion(Quaternion value, Endianness endianness)
    {
        var size = Setup<Quaternion>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadQuaternion();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleQuaternionRef(Quaternion value, Endianness endianness)
    {
        var size = Setup<Quaternion>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        Quaternion read = new();
        reader.ReadQuaternion(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool CharUtf8(char value, Endianness endianness)
    {
        var size = Setup<byte>(endianness, out var writer);
        writer.WriteUtf8Char(value);
        var reader = GetReader(writer);
        var read = reader.ReadUtf8Char();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleGuid(Guid value, Endianness endianness)
    {
        var size = Setup<Guid>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadGuid();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleTimeSpan(TimeSpan value, Endianness endianness)
    {
        var size = Setup<TimeSpan>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadTimeSpan();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleTimeOnly(TimeOnly value, Endianness endianness)
    {
        var size = Setup<TimeOnly>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadTimeOnly();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleDateTime(DateTime value, Endianness endianness)
    {
        const int kindSize = 1;
        var size = Setup<DateTime>(endianness, out var writer) + kindSize;
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadDateTime();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleDateTimeOffset(DateTimeOffset value, Endianness endianness)
    {
        var size = Setup<DateTimeOffset>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadDateTimeOffset();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleDateOnly(DateOnly value, Endianness endianness)
    {
        var size = Setup<DateOnly>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        var read = reader.ReadDateOnly();
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
    public bool UnmanagedStructRef(SimpleStructData value, Endianness endianness)
    {
        var size = Setup<SimpleStructData>(endianness, out var writer);
        writer.WriteStruct(in value);

        var reader = GetReader(writer);
        SimpleStructData read = new();
        reader.ReadStruct(ref read);

        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SerializableObject(SimpleStructData value, Endianness endianness)
    {
        var size = Setup<SimpleStructData>(endianness, out var writer);

        writer.Write(in value);
        writer.WrittenCount.Should().Be(size);

        var reader = GetReader(writer);
        SimpleStructData result = default;
        reader.Read(ref result);
        reader.ReadCount.Should().Be(size);

        return value == result;
    }

    static int readOffset;

    static int Setup<T>(Endianness endianness, out BinaryBufferWriter writer) where T : unmanaged
    {
        var size = Unsafe.SizeOf<T>();
        readOffset = 0;

        ArrayBufferWriter<byte> buffer = new(size);
        writer = new(buffer, endianness);

        return size;
    }

    static BinaryBufferReader GetReader(in BinaryBufferWriter writer)
    {
        var buffer = (ArrayBufferWriter<byte>)writer.Buffer;
        return new(buffer.WrittenSpan, ref readOffset, writer.Endianness);
    }

    [Collection(SerialCollectionDefinition.Name)]
    public class ReadWriteBinaryIntegerTests
    {
        [PropertyTest] public bool TestInt(int value, Endianness endianness) => TestInteger(value, endianness);
        [PropertyTest] public bool TestUInt(uint value, Endianness endianness) => TestInteger(value, endianness);
        [PropertyTest] public bool TestLong(long value, Endianness endianness) => TestInteger(value, endianness);

        [PropertyTest]
        public bool TestULong(ulong value, Endianness endianness) =>
            TestInteger(value, endianness);

        [PropertyTest]
        public bool TestShort(short value, Endianness endianness) =>
            TestInteger(value, endianness);

        [PropertyTest]
        public bool TestUShort(ushort value, Endianness endianness) =>
            TestInteger(value, endianness);

        [PropertyTest] public bool TestByte(byte value, Endianness endianness) => TestInteger(value, endianness);

        [PropertyTest]
        public bool TestSByte(sbyte value, Endianness endianness) =>
            TestInteger(value, endianness);

        static bool TestInteger<T>(T value, Endianness endianness)
            where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
        {
            var size = Setup<T>(endianness, out var writer);
            writer.WriteNumber(value);
            var reader = GetReader(writer);
            var read = reader.ReadNumber<T>();
            reader.ReadCount.Should().Be(size);
            return EqualityComparer<T>.Default.Equals(read, value);
        }
    }

    [Collection(SerialCollectionDefinition.Name)]
    public class ReadWriteNullableTests
    {
        [PropertyTest]
        public bool SingleByte(byte? value, Endianness endianness)
        {
            var size = Setup<byte>(endianness, out var writer);
            size = (value.HasValue ? size : 0) + 1;

            writer.Write(in value);
            var reader = GetReader(writer);
            var read = reader.ReadNullableByte();
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool SingleSByte(sbyte? value, Endianness endianness)
        {
            var size = Setup<sbyte>(endianness, out var writer);
            size = (value.HasValue ? size : 0) + 1;
            writer.Write(value);
            var reader = GetReader(writer);
            var read = reader.ReadNullableSByte();
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool SingleBool(bool? value, Endianness endianness)
        {
            var size = Setup<bool>(endianness, out var writer);
            size = (value.HasValue ? size : 0) + 1;
            writer.Write(value);
            var reader = GetReader(writer);
            var read = reader.ReadNullableBoolean();
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool SingleChar(char? value, Endianness endianness)
        {
            var size = Setup<char>(endianness, out var writer);
            size = (value.HasValue ? size : 0) + 1;
            writer.Write(value);
            var reader = GetReader(writer);
            var read = reader.ReadNullableChar();
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool SingleShort(short? value, Endianness endianness)
        {
            var size = Setup<short>(endianness, out var writer);
            size = (value.HasValue ? size : 0) + 1;
            writer.Write(value);
            var reader = GetReader(writer);
            var read = reader.ReadNullableInt16();
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool SingleUShort(ushort? value, Endianness endianness)
        {
            var size = Setup<ushort>(endianness, out var writer);
            size = (value.HasValue ? size : 0) + 1;
            writer.Write(value);
            var reader = GetReader(writer);
            var read = reader.ReadNullableUInt16();
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool SingleInt(int? value, Endianness endianness)
        {
            var size = Setup<int>(endianness, out var writer);
            size = (value.HasValue ? size : 0) + 1;
            writer.Write(value);
            var reader = GetReader(writer);
            var read = reader.ReadNullableInt32();
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool SingleUInt(uint? value, Endianness endianness)
        {
            var size = Setup<uint>(endianness, out var writer);
            size = (value.HasValue ? size : 0) + 1;
            writer.Write(value);
            var reader = GetReader(writer);
            var read = reader.ReadNullableUInt32();
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool SingleLong(long? value, Endianness endianness)
        {
            var size = Setup<long>(endianness, out var writer);
            size = (value.HasValue ? size : 0) + 1;
            writer.Write(value);
            var reader = GetReader(writer);
            var read = reader.ReadNullableInt64();
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool SingleULong(ulong? value, Endianness endianness)
        {
            var size = Setup<ulong>(endianness, out var writer);
            size = (value.HasValue ? size : 0) + 1;
            writer.Write(value);
            var reader = GetReader(writer);
            var read = reader.ReadNullableUInt64();
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool SingleInt128(Int128? value, Endianness endianness)
        {
            var size = Setup<Int128>(endianness, out var writer);
            size = (value.HasValue ? size : 0) + 1;
            writer.Write(value);
            var reader = GetReader(writer);
            var read = reader.ReadNullableInt128();
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool SingleIntU128(UInt128? value, Endianness endianness)
        {
            var size = Setup<UInt128>(endianness, out var writer);
            size = (value.HasValue ? size : 0) + 1;
            writer.Write(value);
            var reader = GetReader(writer);
            var read = reader.ReadNullableUInt128();
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool SingleHalf(Half? value, Endianness endianness)
        {
            var size = Setup<Half>(endianness, out var writer);
            size = (value.HasValue ? size : 0) + 1;
            writer.Write(value);
            var reader = GetReader(writer);
            var read = reader.ReadNullableHalf();
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool SingleFloat(float? value, Endianness endianness)
        {
            var size = Setup<float>(endianness, out var writer);
            size = (value.HasValue ? size : 0) + 1;
            writer.Write(value);
            var reader = GetReader(writer);
            var read = reader.ReadNullableFloat();
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool SingleDouble(double? value, Endianness endianness)
        {
            var size = Setup<double>(endianness, out var writer);
            size = (value.HasValue ? size : 0) + 1;
            writer.Write(value);
            var reader = GetReader(writer);
            var read = reader.ReadNullableDouble();
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool SingleVector2(Vector2? value, Endianness endianness)
        {
            var size = Setup<Vector2>(endianness, out var writer);
            size = (value.HasValue ? size : 0) + 1;
            writer.Write(value);
            var reader = GetReader(writer);
            var read = reader.ReadNullableVector2();
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool SingleVector3(Vector3? value, Endianness endianness)
        {
            var size = Setup<Vector3>(endianness, out var writer);
            size = (value.HasValue ? size : 0) + 1;
            writer.Write(value);
            var reader = GetReader(writer);
            var read = reader.ReadNullableVector3();
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool SingleVector4(Vector4? value, Endianness endianness)
        {
            var size = Setup<Vector4>(endianness, out var writer);
            size = (value.HasValue ? size : 0) + 1;
            writer.Write(value);
            var reader = GetReader(writer);
            var read = reader.ReadNullableVector4();
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool SingleQuaternion(Quaternion? value, Endianness endianness)
        {
            var size = Setup<Quaternion>(endianness, out var writer);
            size = (value.HasValue ? size : 0) + 1;
            writer.Write(value);
            var reader = GetReader(writer);
            var read = reader.ReadNullableQuaternion();
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool CharUtf8(char? value, Endianness endianness)
        {
            var size = Setup<byte>(endianness, out var writer);
            size = (value.HasValue ? size : 0) + 1;
            writer.WriteUtf8Char(value);
            var reader = GetReader(writer);
            var read = reader.ReadNullableUtf8Char();
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool SingleGuid(Guid? value, Endianness endianness)
        {
            var size = Setup<Guid>(endianness, out var writer);
            size = (value.HasValue ? size : 0) + 1;
            writer.Write(value);
            var reader = GetReader(writer);
            var read = reader.ReadNullableGuid();
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool SingleTimeSpan(TimeSpan? value, Endianness endianness)
        {
            var size = Setup<TimeSpan>(endianness, out var writer);
            size = (value.HasValue ? size : 0) + 1;
            writer.Write(value);
            var reader = GetReader(writer);
            var read = reader.ReadNullableTimeSpan();
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool SingleTimeOnly(TimeOnly? value, Endianness endianness)
        {
            var size = Setup<TimeOnly>(endianness, out var writer);
            size = (value.HasValue ? size : 0) + 1;
            writer.Write(value);
            var reader = GetReader(writer);
            var read = reader.ReadNullableTimeOnly();
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool SingleDateTime(DateTime? value, Endianness endianness)
        {
            const int kindSize = 1;
            var size = Setup<DateTime>(endianness, out var writer) + kindSize;
            size = (value.HasValue ? size : 0) + 1;
            writer.Write(value);
            var reader = GetReader(writer);
            var read = reader.ReadNullableDateTime();
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool SingleDateTimeOffset(DateTimeOffset? value, Endianness endianness)
        {
            var size = Setup<DateTimeOffset>(endianness, out var writer);
            size = (value.HasValue ? size : 0) + 1;
            writer.Write(value);
            var reader = GetReader(writer);
            var read = reader.ReadNullableDateTimeOffset();
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool SingleDateOnly(DateOnly? value, Endianness endianness)
        {
            var size = Setup<DateOnly>(endianness, out var writer);
            size = (value.HasValue ? size : 0) + 1;
            writer.Write(value);
            var reader = GetReader(writer);
            var read = reader.ReadNullableDateOnly();
            reader.ReadCount.Should().Be(size);
            return value == read;
        }

        [PropertyTest]
        public bool UnmanagedStruct(SimpleStructData? value, Endianness endianness)
        {
            var size = Setup<SimpleStructData>(endianness, out var writer);
            size = (value.HasValue ? size : 0) + 1;
            writer.WriteStruct(in value);

            var reader = GetReader(writer);
            var read = reader.ReadNullableStruct<SimpleStructData>();
            reader.ReadCount.Should().Be(size);
            return value == read;
        }
    }
}
