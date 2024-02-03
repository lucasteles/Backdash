using System.Numerics;
using System.Runtime.CompilerServices;
using nGGPO.Core;
using nGGPO.Network;

namespace nGGPO.Serialization;

sealed class PrimitiveBinarySerializer<T> : IBinarySerializer<T> where T : unmanaged
{
    public bool Network { get; init; } = true;

    public readonly int Size = Unsafe.SizeOf<T>();

    public T Deserialize(in ReadOnlySpan<byte> data)
    {
        var value = Mem.ReadUnaligned<T>(data);
        return Network ? Endianness.ToHostOrder(value) : value;
    }

    public int SerializeScoped(scoped ref T data, Span<byte> buffer)
    {
        var reordered = Network ? Endianness.ToNetworkOrder(data) : data;
        return Mem.WriteStruct(reordered, buffer);
    }

    public int Serialize(ref T data, Span<byte> buffer) =>
        SerializeScoped(ref data, buffer);
}

sealed class EnumSerializer<TEnum, TInt>
    : IBinarySerializer<TEnum>
    where TEnum : unmanaged, Enum
    where TInt : unmanaged, IBinaryInteger<TInt>
{
    static readonly PrimitiveBinarySerializer<TInt> valueBinarySerializer = new();

    public TEnum Deserialize(in ReadOnlySpan<byte> data)
    {
        var underValue = valueBinarySerializer.Deserialize(in data);
        return Mem.IntegerAsEnum<TEnum, TInt>(underValue);
    }

    public int Serialize(ref TEnum data, Span<byte> buffer)
    {
        var underValue = Mem.EnumAsInteger<TEnum, TInt>(data);
        return valueBinarySerializer.SerializeScoped(ref underValue, buffer);
    }
}
