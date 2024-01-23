using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using FsCheck.Xunit;
using nGGPO.Serialization;

namespace nGGPO.Tests.Serialization;

public class SerializersTests
{
    [Property]
    public bool ShouldSerializeInt(int value)
    {
        var serializer = BinarySerializers.ForPrimitive<int>();
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [Property]
    public bool ShouldSerializeUInt(uint value)
    {
        var serializer = BinarySerializers.ForPrimitive<uint>();
        Span<byte> buffer = stackalloc byte[sizeof(uint)];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [Property]
    public bool ShouldSerializeULong(ulong value)
    {
        var serializer = BinarySerializers.ForPrimitive<ulong>();
        Span<byte> buffer = stackalloc byte[sizeof(ulong)];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [Property]
    public bool ShouldSerializeLong(long value)
    {
        var serializer = BinarySerializers.ForPrimitive<long>();
        Span<byte> buffer = stackalloc byte[sizeof(long)];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [Property]
    public bool ShouldSerializeShort(short value)
    {
        var serializer = BinarySerializers.ForPrimitive<short>();
        Span<byte> buffer = stackalloc byte[sizeof(short)];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [Property]
    public bool ShouldSerializeUShort(ushort value)
    {
        var serializer = BinarySerializers.ForPrimitive<ushort>();
        Span<byte> buffer = stackalloc byte[sizeof(ushort)];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [Property]
    public bool ShouldSerializeByte(byte value)
    {
        var serializer = BinarySerializers.ForPrimitive<byte>();
        Span<byte> buffer = stackalloc byte[sizeof(byte)];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [Property]
    public bool ShouldSerializeSByte(sbyte value)
    {
        var serializer = BinarySerializers.ForPrimitive<sbyte>();
        Span<byte> buffer = stackalloc byte[sizeof(sbyte)];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }


    [Property]
    public bool ShouldSerializeIntEnum(IntEnum value)
    {
        var serializer = BinarySerializers.ForEnum<IntEnum>();
        Span<byte> buffer = stackalloc byte[sizeof(IntEnum)];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [Property]
    public bool ShouldSerializeUIntEnum(UIntEnum value)
    {
        var serializer = BinarySerializers.ForEnum<UIntEnum>();
        Span<byte> buffer = stackalloc byte[sizeof(UIntEnum)];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [Property]
    public bool ShouldSerializeULongEnum(ULongEnum value)
    {
        var serializer = BinarySerializers.ForEnum<ULongEnum>();
        Span<byte> buffer = stackalloc byte[sizeof(ulong)];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [Property]
    public bool ShouldSerializeLongEnum(LongEnum value)
    {
        var serializer = BinarySerializers.ForEnum<LongEnum>();
        Span<byte> buffer = stackalloc byte[sizeof(LongEnum)];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [Property]
    public bool ShouldSerializeShortEnum(ShortEnum value)
    {
        var serializer = BinarySerializers.ForEnum<ShortEnum>();
        Span<byte> buffer = stackalloc byte[sizeof(ShortEnum)];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [Property]
    public bool ShouldSerializeUShortEnum(UShortEnum value)
    {
        var serializer = BinarySerializers.ForEnum<UShortEnum>();
        Span<byte> buffer = stackalloc byte[sizeof(UShortEnum)];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [Property]
    public bool ShouldSerializeByteEnum(ByteEnum value)
    {
        var serializer = BinarySerializers.ForEnum<ByteEnum>();
        Span<byte> buffer = stackalloc byte[sizeof(ByteEnum)];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [Property]
    public bool ShouldSerializeSByteEnum(SByteEnum value)
    {
        var serializer = BinarySerializers.ForEnum<SByteEnum>();
        Span<byte> buffer = stackalloc byte[sizeof(sbyte)];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeSimpleStruct(SimpleStructData value)
    {
        var serializer = BinarySerializers.ForStructure<SimpleStructData>();
        Span<byte> buffer = stackalloc byte[Unsafe.SizeOf<SimpleStructData>()];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result == value;
    }

    [PropertyTest]
    public bool ShouldSerializeMarshalStruct(MarshalStructData value)
    {
        var serializer = BinarySerializers.ForStructure<MarshalStructData>(marshall: true);
        Span<byte> buffer = stackalloc byte[Marshal.SizeOf<MarshalStructData>()];
        serializer.Serialize(ref value, buffer);
        var result = serializer.Deserialize(buffer);
        return result.IsEquivalent(value);
    }
}
