using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using Backdash.Serialization;
using Backdash.Tests.TestUtils;
using Backdash.Tests.TestUtils.Types;

namespace Backdash.Tests.Specs.Unit.Serialization;

public class SerializersTests
{
    [PropertyTest]
    public bool ShouldSerializeInt(int value)
    {
        var serializer = BinarySerializerFactory.ForInteger<int>();
        serializer.Should().NotBeNull().And.BeOfType<IntegerBinarySerializer<int>>();
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeUInt(uint value)
    {
        var serializer = BinarySerializerFactory.ForInteger<uint>();
        serializer.Should().NotBeNull().And.BeOfType<IntegerBinarySerializer<uint>>();
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeULong(ulong value)
    {
        var serializer = BinarySerializerFactory.ForInteger<ulong>();
        serializer.Should().NotBeNull().And.BeOfType<IntegerBinarySerializer<ulong>>();
        Span<byte> buffer = stackalloc byte[sizeof(ulong)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeLong(long value)
    {
        var serializer = BinarySerializerFactory.ForInteger<long>();
        serializer.Should().NotBeNull().And.BeOfType<IntegerBinarySerializer<long>>();
        Span<byte> buffer = stackalloc byte[sizeof(long)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeShort(short value)
    {
        var serializer = BinarySerializerFactory.ForInteger<short>();
        serializer.Should().NotBeNull().And.BeOfType<IntegerBinarySerializer<short>>();
        Span<byte> buffer = stackalloc byte[sizeof(short)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeUShort(ushort value)
    {
        var serializer = BinarySerializerFactory.ForInteger<ushort>();
        serializer.Should().NotBeNull().And.BeOfType<IntegerBinarySerializer<ushort>>();
        Span<byte> buffer = stackalloc byte[sizeof(ushort)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeByte(byte value)
    {
        var serializer = BinarySerializerFactory.ForInteger<byte>();
        serializer.Should().NotBeNull().And.BeOfType<IntegerBinarySerializer<byte>>();
        Span<byte> buffer = stackalloc byte[sizeof(byte)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeSByte(sbyte value)
    {
        var serializer = BinarySerializerFactory.ForInteger<sbyte>();
        serializer.Should().NotBeNull().And.BeOfType<IntegerBinarySerializer<sbyte>>();
        Span<byte> buffer = stackalloc byte[sizeof(sbyte)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeInt128(Int128 value)
    {
        var serializer = BinarySerializerFactory.ForInteger<Int128>();
        serializer.Should().NotBeNull().And.BeOfType<IntegerBinarySerializer<Int128>>();
        Span<byte> buffer = stackalloc byte[Unsafe.SizeOf<Int128>()];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeIntU128(UInt128 value)
    {
        var serializer = BinarySerializerFactory.ForInteger<UInt128>();
        serializer.Should().NotBeNull().And.BeOfType<IntegerBinarySerializer<UInt128>>();
        Span<byte> buffer = stackalloc byte[Unsafe.SizeOf<UInt128>()];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeIntEnum(IntEnum value)
    {
        var serializer = BinarySerializerFactory.ForEnum<IntEnum>();
        AssertBaseSerializer<IntEnum, int>(serializer);
        Span<byte> buffer = stackalloc byte[sizeof(IntEnum)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeUIntEnum(UIntEnum value)
    {
        var serializer = BinarySerializerFactory.ForEnum<UIntEnum>();
        AssertBaseSerializer<UIntEnum, uint>(serializer);
        Span<byte> buffer = stackalloc byte[sizeof(UIntEnum)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeULongEnum(ULongEnum value)
    {
        var serializer = BinarySerializerFactory.ForEnum<ULongEnum>();
        AssertBaseSerializer<ULongEnum, ulong>(serializer);
        Span<byte> buffer = stackalloc byte[sizeof(ulong)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeLongEnum(LongEnum value)
    {
        var serializer = BinarySerializerFactory.ForEnum<LongEnum>();
        AssertBaseSerializer<LongEnum, long>(serializer);
        Span<byte> buffer = stackalloc byte[sizeof(LongEnum)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeShortEnum(ShortEnum value)
    {
        var serializer = BinarySerializerFactory.ForEnum<ShortEnum>();
        AssertBaseSerializer<ShortEnum, short>(serializer);
        Span<byte> buffer = stackalloc byte[sizeof(ShortEnum)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeUShortEnum(UShortEnum value)
    {
        var serializer = BinarySerializerFactory.ForEnum<UShortEnum>();
        AssertBaseSerializer<UShortEnum, ushort>(serializer);
        Span<byte> buffer = stackalloc byte[sizeof(UShortEnum)];
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
        where TInt : unmanaged, IBinaryInteger<TInt>, IMinMaxValue<TInt> =>
        (serializer as EnumBinarySerializer<T>)?
        .GetBaseSerializer().Should().NotBeNull()
        .And
        .BeOfType<EnumBinarySerializer<T, TInt>>();

    [Fact]
    public void ShouldReturnCorrectSerializerForStruct()
    {
        var serializer = BinarySerializerFactory.Get<SimpleStructData>();
        serializer.Should().BeOfType<StructBinarySerializer<SimpleStructData>>();
    }

    static void AssertIntegerSerializer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>() where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
    {
        var serializer = BinarySerializerFactory.Get<T>();
        serializer.Should().BeOfType<IntegerBinarySerializer<T>>();
    }

    static void AssertEnumSerializer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] T>() where T : unmanaged, Enum
    {
        var serializer = BinarySerializerFactory.Get<T>();
        serializer.Should().BeOfType<EnumBinarySerializer<T>>();
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

    [Fact] public void AssertSerializerByteEnum() => AssertEnumSerializer<ByteEnum>();
    [Fact] public void AssertSerializerSByteEnum() => AssertEnumSerializer<SByteEnum>();
    [Fact] public void AssertSerializerShortEnum() => AssertEnumSerializer<ShortEnum>();
    [Fact] public void AssertSerializerUShortEnum() => AssertEnumSerializer<UShortEnum>();
    [Fact] public void AssertSerializerIntEnum() => AssertEnumSerializer<IntEnum>();
    [Fact] public void AssertSerializerUIntEnum() => AssertEnumSerializer<UIntEnum>();
    [Fact] public void AssertSerializerLongEnum() => AssertEnumSerializer<LongEnum>();
    [Fact] public void AssertSerializerULongEnum() => AssertEnumSerializer<ULongEnum>();

}
