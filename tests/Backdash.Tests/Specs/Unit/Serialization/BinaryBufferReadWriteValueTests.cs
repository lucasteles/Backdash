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
        byte read = 0;
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleSByte(sbyte value, Endianness endianness)
    {
        var size = Setup<sbyte>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        sbyte read = 0;
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleBool(bool value, Endianness endianness)
    {
        var size = Setup<bool>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        bool read = false;
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleChar(char value, Endianness endianness)
    {
        var size = Setup<char>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        char read = '\0';
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleShort(short value, Endianness endianness)
    {
        var size = Setup<short>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        short read = 0;
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleUShort(ushort value, Endianness endianness)
    {
        var size = Setup<ushort>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        ushort read = 0;
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleInt(int value, Endianness endianness)
    {
        var size = Setup<int>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        int read = 0;
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleUInt(uint value, Endianness endianness)
    {
        var size = Setup<uint>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        uint read = 0;
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleLong(long value, Endianness endianness)
    {
        var size = Setup<long>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        long read = 0;
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleULong(ulong value, Endianness endianness)
    {
        var size = Setup<ulong>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        ulong read = 0;
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleInt128(Int128 value, Endianness endianness)
    {
        var size = Setup<Int128>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        Int128 read = 0;
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleIntU128(UInt128 value, Endianness endianness)
    {
        var size = Setup<UInt128>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        UInt128 read = 0;
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleHalf(Half value, Endianness endianness)
    {
        var size = Setup<Half>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        Half read = default;
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleFloat(float value, Endianness endianness)
    {
        var size = Setup<float>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        float read = 0f;
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleDouble(double value, Endianness endianness)
    {
        var size = Setup<double>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        double read = 0;
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleVector2(Vector2 value, Endianness endianness)
    {
        var size = Setup<Vector2>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        Vector2 read = default;
        reader.Read(ref read);
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
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleVector3(Vector3 value, Endianness endianness)
    {
        var size = Setup<Vector3>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        Vector3 read = default;
        reader.Read(ref read);
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
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleVector4(Vector4 value, Endianness endianness)
    {
        var size = Setup<Vector4>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        Vector4 read = default;
        reader.Read(ref read);
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
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleQuaternion(Quaternion value, Endianness endianness)
    {
        var size = Setup<Quaternion>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        Quaternion read = default;
        reader.Read(ref read);
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
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleGuid(Guid value, Endianness endianness)
    {
        var size = Setup<Guid>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        Guid read = Guid.Empty;
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleTimeSpan(TimeSpan value, Endianness endianness)
    {
        var size = Setup<TimeSpan>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        TimeSpan read = TimeSpan.Zero;
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleTimeOnly(TimeOnly value, Endianness endianness)
    {
        var size = Setup<TimeOnly>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        TimeOnly read = default;
        reader.Read(ref read);
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
        DateTime read = default;
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleDateTimeOffset(DateTimeOffset value, Endianness endianness)
    {
        var size = Setup<DateTimeOffset>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        DateTimeOffset read = default;
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleDateOnly(DateOnly value, Endianness endianness)
    {
        var size = Setup<DateOnly>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        DateOnly read = default;
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
            T read = default;
            reader.ReadNumber(ref read);
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
