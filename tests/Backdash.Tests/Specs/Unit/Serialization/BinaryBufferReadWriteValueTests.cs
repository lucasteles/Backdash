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
    public bool SingleByte(byte value, byte read, Endianness endianness)
    {
        var size = Setup<byte>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleSByte(sbyte value, sbyte read, Endianness endianness)
    {
        var size = Setup<sbyte>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleBool(bool value, bool read, Endianness endianness)
    {
        var size = Setup<bool>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleChar(char value, char read, Endianness endianness)
    {
        var size = Setup<char>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleShort(short value, short read, Endianness endianness)
    {
        var size = Setup<short>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleUShort(ushort value, ushort read, Endianness endianness)
    {
        var size = Setup<ushort>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleInt(int value, int read, Endianness endianness)
    {
        var size = Setup<int>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleUInt(uint value, uint read, Endianness endianness)
    {
        var size = Setup<uint>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleLong(long value, long read, Endianness endianness)
    {
        var size = Setup<long>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleULong(ulong value, ulong read, Endianness endianness)
    {
        var size = Setup<ulong>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleInt128(Int128 value, Int128 read, Endianness endianness)
    {
        var size = Setup<Int128>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleIntU128(UInt128 value, UInt128 read, Endianness endianness)
    {
        var size = Setup<UInt128>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleHalf(Half value, Half read, Endianness endianness)
    {
        var size = Setup<Half>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleFloat(float value, float read, Endianness endianness)
    {
        var size = Setup<float>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleDouble(double value, double read, Endianness endianness)
    {
        var size = Setup<double>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
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
        var read = reader.ReadVector2();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleVector2Ref(Vector2 value, Vector2 read, Endianness endianness)
    {
        var size = Setup<Vector2>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
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
        var read = reader.ReadVector3();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleVector3Ref(Vector3 value, Vector3 read, Endianness endianness)
    {
        var size = Setup<Vector3>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
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
        var read = reader.ReadVector4();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleVector4Ref(Vector4 value, Vector4 read, Endianness endianness)
    {
        var size = Setup<Vector4>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
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
        var read = reader.ReadQuaternion();
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleQuaternionRef(Quaternion value, Quaternion read, Endianness endianness)
    {
        var size = Setup<Quaternion>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleGuid(Guid value, Guid read, Endianness endianness)
    {
        var size = Setup<Guid>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleTimeSpan(TimeSpan value, TimeSpan read, Endianness endianness)
    {
        var size = Setup<TimeSpan>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleTimeOnly(TimeOnly value, TimeOnly read, Endianness endianness)
    {
        var size = Setup<TimeOnly>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleDateTime(DateTime value, DateTime read, Endianness endianness)
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
    public bool SingleDateTimeOffset(DateTimeOffset value, DateTimeOffset read, Endianness endianness)
    {
        var size = Setup<DateTimeOffset>(endianness, out var writer);
        writer.Write(value);
        var reader = GetReader(writer);
        reader.Read(ref read);
        reader.ReadCount.Should().Be(size);
        return value == read;
    }

    [PropertyTest]
    public bool SingleDateOnly(DateOnly value, DateOnly read, Endianness endianness)
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

    static int readOffset;

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

    [Collection(SerialCollectionDefinition.Name)]
    public class ReadWriteBinaryIntegerTests
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
}
