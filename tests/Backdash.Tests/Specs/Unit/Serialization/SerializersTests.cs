using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Backdash.Serialization;
using Backdash.Tests.TestUtils;
using Backdash.Tests.TestUtils.Types;

namespace Backdash.Tests.Specs.Unit.Serialization;

public class SerializersTests
{
    [PropertyTest]
    public bool ShouldSerializeInt(int value)
    {
        var serializer = BinarySerializerFactory.Get<int>()!;
        serializer.Should().NotBeNull().And.BeOfType<IntegerBinarySerializer<int>>();
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeUInt(uint value)
    {
        var serializer = BinarySerializerFactory.Get<uint>()!;
        serializer.Should().NotBeNull().And.BeOfType<IntegerBinarySerializer<uint>>();
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeULong(ulong value)
    {
        var serializer = BinarySerializerFactory.Get<ulong>()!;
        serializer.Should().NotBeNull().And.BeOfType<IntegerBinarySerializer<ulong>>();
        Span<byte> buffer = stackalloc byte[sizeof(ulong)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeLong(long value)
    {
        var serializer = BinarySerializerFactory.Get<long>()!;
        serializer.Should().NotBeNull().And.BeOfType<IntegerBinarySerializer<long>>();
        Span<byte> buffer = stackalloc byte[sizeof(long)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeShort(short value)
    {
        var serializer = BinarySerializerFactory.Get<short>()!;
        serializer.Should().NotBeNull().And.BeOfType<IntegerBinarySerializer<short>>();
        Span<byte> buffer = stackalloc byte[sizeof(short)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeUShort(ushort value)
    {
        var serializer = BinarySerializerFactory.Get<ushort>()!;
        serializer.Should().NotBeNull().And.BeOfType<IntegerBinarySerializer<ushort>>();
        Span<byte> buffer = stackalloc byte[sizeof(ushort)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeByte(byte value)
    {
        var serializer = BinarySerializerFactory.Get<byte>()!;
        serializer.Should().NotBeNull().And.BeOfType<IntegerBinarySerializer<byte>>();
        Span<byte> buffer = stackalloc byte[sizeof(byte)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeSByte(sbyte value)
    {
        var serializer = BinarySerializerFactory.Get<sbyte>()!;
        serializer.Should().NotBeNull().And.BeOfType<IntegerBinarySerializer<sbyte>>();
        Span<byte> buffer = stackalloc byte[sizeof(sbyte)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeInt128(Int128 value)
    {
        var serializer = BinarySerializerFactory.Get<Int128>()!;
        serializer.Should().NotBeNull().And.BeOfType<IntegerBinarySerializer<Int128>>();
        Span<byte> buffer = stackalloc byte[Unsafe.SizeOf<Int128>()];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeIntU128(UInt128 value)
    {
        var serializer = BinarySerializerFactory.Get<UInt128>()!;
        serializer.Should().NotBeNull().And.BeOfType<IntegerBinarySerializer<UInt128>>();
        Span<byte> buffer = stackalloc byte[Unsafe.SizeOf<UInt128>()];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeIntEnum(IntEnum value)
    {
        var serializer = BinarySerializerFactory.Get<IntEnum>()!;
        serializer.Should().NotBeNull().And.BeOfType<EnumBinarySerializer<IntEnum, int>>();
        Span<byte> buffer = stackalloc byte[sizeof(IntEnum)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeUIntEnum(UIntEnum value)
    {
        var serializer = BinarySerializerFactory.Get<UIntEnum>()!;
        serializer.Should().NotBeNull().And.BeOfType<EnumBinarySerializer<UIntEnum, uint>>();
        Span<byte> buffer = stackalloc byte[sizeof(UIntEnum)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeULongEnum(ULongEnum value)
    {
        var serializer = BinarySerializerFactory.Get<ULongEnum>()!;
        serializer.Should().NotBeNull().And.BeOfType<EnumBinarySerializer<ULongEnum, ulong>>();
        Span<byte> buffer = stackalloc byte[sizeof(ulong)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeLongEnum(LongEnum value)
    {
        var serializer = BinarySerializerFactory.Get<LongEnum>()!;
        serializer.Should().NotBeNull().And.BeOfType<EnumBinarySerializer<LongEnum, long>>();
        Span<byte> buffer = stackalloc byte[sizeof(LongEnum)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeShortEnum(ShortEnum value)
    {
        var serializer = BinarySerializerFactory.Get<ShortEnum>()!;
        serializer.Should().NotBeNull().And.BeOfType<EnumBinarySerializer<ShortEnum, short>>();
        Span<byte> buffer = stackalloc byte[sizeof(ShortEnum)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeUShortEnum(UShortEnum value)
    {
        var serializer = BinarySerializerFactory.Get<UShortEnum>()!;
        serializer.Should().NotBeNull().And.BeOfType<EnumBinarySerializer<UShortEnum, ushort>>();
        Span<byte> buffer = stackalloc byte[sizeof(UShortEnum)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeByteEnum(ByteEnum value)
    {
        var serializer = BinarySerializerFactory.Get<ByteEnum>()!;
        serializer.Should().NotBeNull().And.BeOfType<EnumBinarySerializer<ByteEnum, byte>>();
        Span<byte> buffer = stackalloc byte[sizeof(ByteEnum)];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeSByteEnum(SByteEnum value)
    {
        var serializer = BinarySerializerFactory.Get<SByteEnum>()!;
        serializer.Should().NotBeNull().And.BeOfType<EnumBinarySerializer<SByteEnum, sbyte>>();
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

    [PropertyTest]
    public bool ShouldSerializeMarshalStruct(MarshalStructData value)
    {
        var serializer = BinarySerializerFactory.ForStruct<MarshalStructData>(marshall: true);
        Span<byte> buffer = stackalloc byte[Marshal.SizeOf<MarshalStructData>()];
        serializer.Serialize(in value, buffer);
        var result = serializer.Deserialize(buffer);
        return result.IsEquivalent(value);
    }
}
