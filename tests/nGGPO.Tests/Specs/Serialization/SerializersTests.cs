using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using nGGPO.Serialization;

namespace nGGPO.Tests.Specs.Serialization;

public class SerializersTests
{
    [PropertyTest]
    public bool ShouldSerializeInt(int value)
    {
        var serializer = BinarySerializerFactory.ForPrimitive<int>();
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeUInt(uint value)
    {
        var serializer = BinarySerializerFactory.ForPrimitive<uint>();
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeULong(ulong value)
    {
        var serializer = BinarySerializerFactory.ForPrimitive<ulong>();
        Span<byte> buffer = stackalloc byte[sizeof(ulong)];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeLong(long value)
    {
        var serializer = BinarySerializerFactory.ForPrimitive<long>();
        Span<byte> buffer = stackalloc byte[sizeof(long)];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeShort(short value)
    {
        var serializer = BinarySerializerFactory.ForPrimitive<short>();
        Span<byte> buffer = stackalloc byte[sizeof(short)];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeUShort(ushort value)
    {
        var serializer = BinarySerializerFactory.ForPrimitive<ushort>();
        Span<byte> buffer = stackalloc byte[sizeof(ushort)];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeByte(byte value)
    {
        var serializer = BinarySerializerFactory.ForPrimitive<byte>();
        Span<byte> buffer = stackalloc byte[sizeof(byte)];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeSByte(sbyte value)
    {
        var serializer = BinarySerializerFactory.ForPrimitive<sbyte>();
        Span<byte> buffer = stackalloc byte[sizeof(sbyte)];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }


    [PropertyTest]
    public bool ShouldSerializeIntEnum(IntEnum value)
    {
        var serializer = BinarySerializerFactory.ForEnum<IntEnum>();
        Span<byte> buffer = stackalloc byte[sizeof(IntEnum)];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeUIntEnum(UIntEnum value)
    {
        var serializer = BinarySerializerFactory.ForEnum<UIntEnum>();
        Span<byte> buffer = stackalloc byte[sizeof(UIntEnum)];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeULongEnum(ULongEnum value)
    {
        var serializer = BinarySerializerFactory.ForEnum<ULongEnum>();
        Span<byte> buffer = stackalloc byte[sizeof(ulong)];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeLongEnum(LongEnum value)
    {
        var serializer = BinarySerializerFactory.ForEnum<LongEnum>();
        Span<byte> buffer = stackalloc byte[sizeof(LongEnum)];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeShortEnum(ShortEnum value)
    {
        var serializer = BinarySerializerFactory.ForEnum<ShortEnum>();
        Span<byte> buffer = stackalloc byte[sizeof(ShortEnum)];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeUShortEnum(UShortEnum value)
    {
        var serializer = BinarySerializerFactory.ForEnum<UShortEnum>();
        Span<byte> buffer = stackalloc byte[sizeof(UShortEnum)];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeByteEnum(ByteEnum value)
    {
        var serializer = BinarySerializerFactory.ForEnum<ByteEnum>();
        Span<byte> buffer = stackalloc byte[sizeof(ByteEnum)];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeSByteEnum(SByteEnum value)
    {
        var serializer = BinarySerializerFactory.ForEnum<SByteEnum>();
        Span<byte> buffer = stackalloc byte[sizeof(sbyte)];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeSimpleStruct(SimpleStructData value)
    {
        var serializer = BinarySerializerFactory.ForStruct<SimpleStructData>();
        Span<byte> buffer = stackalloc byte[Unsafe.SizeOf<SimpleStructData>()];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeMarshalStruct(MarshalStructData value)
    {
        var serializer = BinarySerializerFactory.ForStruct<MarshalStructData>(marshall: true);
        Span<byte> buffer = stackalloc byte[Marshal.SizeOf<MarshalStructData>()];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result.IsEquivalent(value);
    }
}
