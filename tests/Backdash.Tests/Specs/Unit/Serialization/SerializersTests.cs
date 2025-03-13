using System.Numerics;
using System.Runtime.CompilerServices;
using Backdash.Network;
using Backdash.Serialization;
using Backdash.Serialization.Internal;
using Backdash.Tests.TestUtils;
using Backdash.Tests.TestUtils.Types;

namespace Backdash.Tests.Specs.Unit.Serialization;

public class SerializersTests
{
    public class SerializeIntegerBigEndianTests
    {
        const Endianness Endianness = Backdash.Network.Endianness.BigEndian;

        [PropertyTest]
        public bool ShouldSerializeInt(int value)
        {
            var serializer = BinarySerializerFactory.ForInteger<int>(Endianness);
            serializer.Should().NotBeNull().And.BeOfType<IntegerBinaryBigEndianSerializer<int>>();
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            serializer.Serialize(in value, buffer);
            var result = serializer.Deserialize(buffer);
            return result == value;
        }

        [PropertyTest]
        public bool ShouldSerializeUInt(uint value)
        {
            var serializer = BinarySerializerFactory.ForInteger<uint>(Endianness);
            serializer.Should().NotBeNull().And.BeOfType<IntegerBinaryBigEndianSerializer<uint>>();
            Span<byte> buffer = stackalloc byte[sizeof(uint)];
            serializer.Serialize(in value, buffer);
            var result = serializer.Deserialize(buffer);
            return result == value;
        }

        [PropertyTest]
        public bool ShouldSerializeULong(ulong value)
        {
            var serializer = BinarySerializerFactory.ForInteger<ulong>(Endianness);
            serializer.Should().NotBeNull().And.BeOfType<IntegerBinaryBigEndianSerializer<ulong>>();
            Span<byte> buffer = stackalloc byte[sizeof(ulong)];
            serializer.Serialize(in value, buffer);
            var result = serializer.Deserialize(buffer);
            return result == value;
        }

        [PropertyTest]
        public bool ShouldSerializeLong(long value)
        {
            var serializer = BinarySerializerFactory.ForInteger<long>(Endianness);
            serializer.Should().NotBeNull().And.BeOfType<IntegerBinaryBigEndianSerializer<long>>();
            Span<byte> buffer = stackalloc byte[sizeof(long)];
            serializer.Serialize(in value, buffer);
            var result = serializer.Deserialize(buffer);
            return result == value;
        }

        [PropertyTest]
        public bool ShouldSerializeShort(short value)
        {
            var serializer = BinarySerializerFactory.ForInteger<short>(Endianness);
            serializer.Should().NotBeNull().And.BeOfType<IntegerBinaryBigEndianSerializer<short>>();
            Span<byte> buffer = stackalloc byte[sizeof(short)];
            serializer.Serialize(in value, buffer);
            var result = serializer.Deserialize(buffer);
            return result == value;
        }

        [PropertyTest]
        public bool ShouldSerializeUShort(ushort value)
        {
            var serializer = BinarySerializerFactory.ForInteger<ushort>(Endianness);
            serializer.Should().NotBeNull().And.BeOfType<IntegerBinaryBigEndianSerializer<ushort>>();
            Span<byte> buffer = stackalloc byte[sizeof(ushort)];
            serializer.Serialize(in value, buffer);
            var result = serializer.Deserialize(buffer);
            return result == value;
        }

        [PropertyTest]
        public bool ShouldSerializeByte(byte value)
        {
            var serializer = BinarySerializerFactory.ForInteger<byte>(Endianness);
            serializer.Should().NotBeNull().And.BeOfType<IntegerBinaryBigEndianSerializer<byte>>();
            Span<byte> buffer = stackalloc byte[sizeof(byte)];
            serializer.Serialize(in value, buffer);
            var result = serializer.Deserialize(buffer);
            return result == value;
        }

        [PropertyTest]
        public bool ShouldSerializeSByte(sbyte value)
        {
            var serializer = BinarySerializerFactory.ForInteger<sbyte>(Endianness);
            serializer.Should().NotBeNull().And.BeOfType<IntegerBinaryBigEndianSerializer<sbyte>>();
            Span<byte> buffer = stackalloc byte[sizeof(sbyte)];
            serializer.Serialize(in value, buffer);
            var result = serializer.Deserialize(buffer);
            return result == value;
        }

        [PropertyTest]
        public bool ShouldSerializeInt128(Int128 value)
        {
            var serializer = BinarySerializerFactory.ForInteger<Int128>(Endianness);
            serializer.Should().NotBeNull().And.BeOfType<IntegerBinaryBigEndianSerializer<Int128>>();
            Span<byte> buffer = stackalloc byte[Unsafe.SizeOf<Int128>()];
            serializer.Serialize(in value, buffer);
            var result = serializer.Deserialize(buffer);
            return result == value;
        }

        [PropertyTest]
        public bool ShouldSerializeIntU128(UInt128 value)
        {
            var serializer = BinarySerializerFactory.ForInteger<UInt128>(Endianness);
            serializer.Should().NotBeNull().And.BeOfType<IntegerBinaryBigEndianSerializer<UInt128>>();
            Span<byte> buffer = stackalloc byte[Unsafe.SizeOf<UInt128>()];
            serializer.Serialize(in value, buffer);
            var result = serializer.Deserialize(buffer);
            return result == value;
        }
    }

    public class SerializeIntegerLittleEndianTests
    {
        const Endianness Endianness = Backdash.Network.Endianness.LittleEndian;

        [PropertyTest]
        public bool ShouldSerializeInt(int value)
        {
            var serializer = BinarySerializerFactory.ForInteger<int>(Endianness);
            serializer.Should().NotBeNull().And.BeOfType<IntegerBinaryLittleEndianSerializer<int>>();
            Span<byte> buffer = stackalloc byte[sizeof(int)];
            serializer.Serialize(in value, buffer);
            var result = serializer.Deserialize(buffer);
            return result == value;
        }

        [PropertyTest]
        public bool ShouldSerializeUInt(uint value)
        {
            var serializer = BinarySerializerFactory.ForInteger<uint>(Endianness);
            serializer.Should().NotBeNull().And.BeOfType<IntegerBinaryLittleEndianSerializer<uint>>();
            Span<byte> buffer = stackalloc byte[sizeof(uint)];
            serializer.Serialize(in value, buffer);
            var result = serializer.Deserialize(buffer);
            return result == value;
        }

        [PropertyTest]
        public bool ShouldSerializeULong(ulong value)
        {
            var serializer = BinarySerializerFactory.ForInteger<ulong>(Endianness);
            serializer.Should().NotBeNull().And.BeOfType<IntegerBinaryLittleEndianSerializer<ulong>>();
            Span<byte> buffer = stackalloc byte[sizeof(ulong)];
            serializer.Serialize(in value, buffer);
            var result = serializer.Deserialize(buffer);
            return result == value;
        }

        [PropertyTest]
        public bool ShouldSerializeLong(long value)
        {
            var serializer = BinarySerializerFactory.ForInteger<long>(Endianness);
            serializer.Should().NotBeNull().And.BeOfType<IntegerBinaryLittleEndianSerializer<long>>();
            Span<byte> buffer = stackalloc byte[sizeof(long)];
            serializer.Serialize(in value, buffer);
            var result = serializer.Deserialize(buffer);
            return result == value;
        }

        [PropertyTest]
        public bool ShouldSerializeShort(short value)
        {
            var serializer = BinarySerializerFactory.ForInteger<short>(Endianness);
            serializer.Should().NotBeNull().And.BeOfType<IntegerBinaryLittleEndianSerializer<short>>();
            Span<byte> buffer = stackalloc byte[sizeof(short)];
            serializer.Serialize(in value, buffer);
            var result = serializer.Deserialize(buffer);
            return result == value;
        }

        [PropertyTest]
        public bool ShouldSerializeUShort(ushort value)
        {
            var serializer = BinarySerializerFactory.ForInteger<ushort>(Endianness);
            serializer.Should().NotBeNull().And.BeOfType<IntegerBinaryLittleEndianSerializer<ushort>>();
            Span<byte> buffer = stackalloc byte[sizeof(ushort)];
            serializer.Serialize(in value, buffer);
            var result = serializer.Deserialize(buffer);
            return result == value;
        }

        [PropertyTest]
        public bool ShouldSerializeByte(byte value)
        {
            var serializer = BinarySerializerFactory.ForInteger<byte>(Endianness);
            serializer.Should().NotBeNull().And.BeOfType<IntegerBinaryLittleEndianSerializer<byte>>();
            Span<byte> buffer = stackalloc byte[sizeof(byte)];
            serializer.Serialize(in value, buffer);
            var result = serializer.Deserialize(buffer);
            return result == value;
        }

        [PropertyTest]
        public bool ShouldSerializeSByte(sbyte value)
        {
            var serializer = BinarySerializerFactory.ForInteger<sbyte>(Endianness);
            serializer.Should().NotBeNull().And.BeOfType<IntegerBinaryLittleEndianSerializer<sbyte>>();
            Span<byte> buffer = stackalloc byte[sizeof(sbyte)];
            serializer.Serialize(in value, buffer);
            var result = serializer.Deserialize(buffer);
            return result == value;
        }

        [PropertyTest]
        public bool ShouldSerializeInt128(Int128 value)
        {
            var serializer = BinarySerializerFactory.ForInteger<Int128>(Endianness);
            serializer.Should().NotBeNull().And.BeOfType<IntegerBinaryLittleEndianSerializer<Int128>>();
            Span<byte> buffer = stackalloc byte[Unsafe.SizeOf<Int128>()];
            serializer.Serialize(in value, buffer);
            var result = serializer.Deserialize(buffer);
            return result == value;
        }

        [PropertyTest]
        public bool ShouldSerializeIntU128(UInt128 value)
        {
            var serializer = BinarySerializerFactory.ForInteger<UInt128>(Endianness);
            serializer.Should().NotBeNull().And.BeOfType<IntegerBinaryLittleEndianSerializer<UInt128>>();
            Span<byte> buffer = stackalloc byte[Unsafe.SizeOf<UInt128>()];
            serializer.Serialize(in value, buffer);
            var result = serializer.Deserialize(buffer);
            return result == value;
        }
    }

    [PropertyTest]
    public bool ShouldSerializeIntEnum(Int32Enum value)
    {
        var serializer = BinarySerializerFactory.ForEnum<Int32Enum>();
        AssertBaseSerializer<Int32Enum, int>(serializer);
        Span<byte> buffer = stackalloc byte[sizeof(Int32Enum)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeUIntEnum(UInt32Enum value)
    {
        var serializer = BinarySerializerFactory.ForEnum<UInt32Enum>();
        AssertBaseSerializer<UInt32Enum, uint>(serializer);
        Span<byte> buffer = stackalloc byte[sizeof(UInt32Enum)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeULongEnum(UInt64Enum value)
    {
        var serializer = BinarySerializerFactory.ForEnum<UInt64Enum>();
        AssertBaseSerializer<UInt64Enum, ulong>(serializer);
        Span<byte> buffer = stackalloc byte[sizeof(ulong)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeLongEnum(Int64Enum value)
    {
        var serializer = BinarySerializerFactory.ForEnum<Int64Enum>();
        AssertBaseSerializer<Int64Enum, long>(serializer);
        Span<byte> buffer = stackalloc byte[sizeof(Int64Enum)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeShortEnum(Int16Enum value)
    {
        var serializer = BinarySerializerFactory.ForEnum<Int16Enum>();
        AssertBaseSerializer<Int16Enum, short>(serializer);
        Span<byte> buffer = stackalloc byte[sizeof(Int16Enum)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeUShortEnum(UInt16Enum value)
    {
        var serializer = BinarySerializerFactory.ForEnum<UInt16Enum>();
        AssertBaseSerializer<UInt16Enum, ushort>(serializer);
        Span<byte> buffer = stackalloc byte[sizeof(UInt16Enum)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeByteEnum(ByteEnum value)
    {
        var serializer = BinarySerializerFactory.ForEnum<ByteEnum>();
        AssertBaseSerializer<ByteEnum, byte>(serializer);
        Span<byte> buffer = stackalloc byte[sizeof(ByteEnum)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeSByteEnum(SByteEnum value)
    {
        var serializer = BinarySerializerFactory.ForEnum<SByteEnum>();
        AssertBaseSerializer<SByteEnum, sbyte>(serializer);
        Span<byte> buffer = stackalloc byte[sizeof(sbyte)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeSimpleStruct(SimpleStructData value)
    {
        var serializer = BinarySerializerFactory.ForStruct<SimpleStructData>();
        Span<byte> buffer = stackalloc byte[Unsafe.SizeOf<SimpleStructData>()];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    static void AssertBaseSerializer<T, TInt>(IBinarySerializer<T> serializer)
        where T : unmanaged, Enum
        where TInt : unmanaged, IBinaryInteger<TInt>, IMinMaxValue<TInt>
    {
        var baseSerializer = (serializer as EnumBinarySerializer<T, TInt>)?.GetBaseSerializer();

        baseSerializer.Should().NotBeNull();

        switch (serializer.Endianness)
        {
            case Endianness.LittleEndian:
                baseSerializer.Should().BeOfType<IntegerBinaryLittleEndianSerializer<TInt>>();
                break;
            case Endianness.BigEndian:
                baseSerializer.Should().BeOfType<IntegerBinaryBigEndianSerializer<TInt>>();
                break;
            default:
                Assert.Fail("Invalid endianness");
                break;
        }
    }

    [Fact]
    public void ShouldReturnCorrectSerializerForStruct()
    {
        var serializer = BinarySerializerFactory.ForStruct<SimpleStructData>();
        serializer.Should().BeOfType<StructBinarySerializer<SimpleStructData>>();
    }

    static void AssertIntegerSerializer<T>() where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
    {
        var serializer = BinarySerializerFactory.ForInteger<T>(Endianness.BigEndian);
        serializer.Should().BeOfType<IntegerBinaryBigEndianSerializer<T>>();
    }

    static void AssertEnumSerializer<T, TInt>()
        where T : unmanaged, Enum
        where TInt : unmanaged, IBinaryInteger<TInt>, IMinMaxValue<TInt>
    {
        var serializer = BinarySerializerFactory.ForEnum<T>();
        serializer.Should().BeOfType<EnumBinarySerializer<T, TInt>>();
    }

    [Fact] public void AssertSerializerByte() => AssertIntegerSerializer<byte>();
    [Fact] public void AssertSerializerSByte() => AssertIntegerSerializer<sbyte>();
    [Fact] public void AssertSerializerShort() => AssertIntegerSerializer<short>();
    [Fact] public void AssertSerializerUShort() => AssertIntegerSerializer<ushort>();
    [Fact] public void AssertSerializerInt() => AssertIntegerSerializer<int>();
    [Fact] public void AssertSerializerUInt() => AssertIntegerSerializer<uint>();
    [Fact] public void AssertSerializerLong() => AssertIntegerSerializer<long>();
    [Fact] public void AssertSerializerULong() => AssertIntegerSerializer<ulong>();
    [Fact] public void AssertSerializerInt128() => AssertIntegerSerializer<Int128>();
    [Fact] public void AssertSerializerUInt128() => AssertIntegerSerializer<UInt128>();

    [Fact] public void AssertSerializerByteEnum() => AssertEnumSerializer<ByteEnum, byte>();
    [Fact] public void AssertSerializerSByteEnum() => AssertEnumSerializer<SByteEnum, sbyte>();
    [Fact] public void AssertSerializerShortEnum() => AssertEnumSerializer<Int16Enum, short>();
    [Fact] public void AssertSerializerUShortEnum() => AssertEnumSerializer<UInt16Enum, ushort>();
    [Fact] public void AssertSerializerIntEnum() => AssertEnumSerializer<Int32Enum, int>();
    [Fact] public void AssertSerializerUIntEnum() => AssertEnumSerializer<UInt32Enum, uint>();
    [Fact] public void AssertSerializerLongEnum() => AssertEnumSerializer<Int64Enum, long>();
    [Fact] public void AssertSerializerULongEnum() => AssertEnumSerializer<UInt64Enum, ulong>();
}

static file class Extensions
{
    public static T Deserialize<T>(this IBinarySerializer<T> serializer, ReadOnlySpan<byte> data) where T : new()
    {
        var result = new T();
        serializer.Deserialize(data, ref result);
        return result;
    }
}
