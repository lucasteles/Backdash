using System.Numerics;
using System.Runtime.CompilerServices;
using Backdash.Core;
using Backdash.Network;

namespace Backdash.Serialization;

sealed class PrimitiveBinarySerializer<T> : IBinarySerializer<T> where T : unmanaged
{
    public int Serialize(in T data, Span<byte> buffer) => Mem.WriteStruct(in data, buffer);

    public void Deserialize(ReadOnlySpan<byte> data, ref T value)
    {
        ref var readValue = ref Mem.ReadUnaligned<T>(data);
        value = ref readValue;
    }
}

sealed class IntegerBinarySerializer<T>(Endianness endianness)
    : IBinarySerializer<T> where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T>
{
    readonly bool isUnsigned = T.IsZero(T.MinValue);

    public int Serialize(in T data, Span<byte> buffer)
    {
        int size;
        ref var valueRef = ref Unsafe.AsRef(in data);

        return endianness switch
        {
            Endianness.BigEndian => valueRef.TryWriteBigEndian(buffer, out size) ? size : 0,
            Endianness.LittleEndian => valueRef.TryWriteLittleEndian(buffer, out size) ? size : 0,
            _ => throw new BackdashException("Invalid integer serialization mode")
        };
    }

    public void Deserialize(ReadOnlySpan<byte> data, ref T value)
    {
        switch (endianness)
        {
            case Endianness.BigEndian:
                value = T.ReadBigEndian(data, isUnsigned);
                return;
            case Endianness.LittleEndian:
                value = T.ReadLittleEndian(data, isUnsigned);
                break;
            default:
                throw new BackdashException("Invalid integer serialization mode");
        }
    }
}

sealed class EnumBinarySerializer<TEnum, TInt>(IBinarySerializer<TInt> serializer) : IBinarySerializer<TEnum>
    where TEnum : unmanaged, Enum
    where TInt : unmanaged, IBinaryInteger<TInt>
{
    public int Serialize(in TEnum data, Span<byte> buffer)
    {
        ref var underValue = ref Mem.EnumAsInteger<TEnum, TInt>(ref Unsafe.AsRef(in data));
        return serializer.Serialize(in underValue, buffer);
    }

    public void Deserialize(ReadOnlySpan<byte> data, ref TEnum value)
    {
        ref var underValue = ref Mem.EnumAsInteger<TEnum, TInt>(ref value);
        serializer.Deserialize(data, ref underValue);
        value = ref Mem.IntegerAsEnum<TEnum, TInt>(ref underValue);
    }
}
