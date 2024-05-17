using System.Numerics;
using System.Runtime.CompilerServices;
using Backdash.Core;
using Backdash.Network;

namespace Backdash.Serialization;

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
            _ => throw new NetcodeException("Invalid integer serialization mode")
        };
    }

    public int Deserialize(ReadOnlySpan<byte> data, ref T value)
    {
        var size = Unsafe.SizeOf<T>();
        value = endianness switch
        {
            Endianness.BigEndian => T.ReadBigEndian(data[..size], isUnsigned),
            Endianness.LittleEndian => T.ReadLittleEndian(data[..size], isUnsigned),
            _ => throw new NetcodeException("Invalid integer serialization mode"),
        };
        return size;
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

    public int Deserialize(ReadOnlySpan<byte> data, ref TEnum value)
    {
        ref var underValue = ref Mem.EnumAsInteger<TEnum, TInt>(ref value);
        var size = serializer.Deserialize(data, ref underValue);
        value = ref Mem.IntegerAsEnum<TEnum, TInt>(ref underValue);
        return size;
    }
}

sealed class EnumBinarySerializer<TEnum>(Endianness endianness) : IBinarySerializer<TEnum>
    where TEnum : unmanaged, Enum
{
    readonly IBinarySerializer<TEnum> serializer = Type.GetTypeCode(typeof(TEnum)) switch
    {
        TypeCode.Int32 => new EnumBinarySerializer<TEnum, int>(
            new IntegerBinarySerializer<int>(endianness)),
        TypeCode.UInt32 => new EnumBinarySerializer<TEnum, uint>(
            new IntegerBinarySerializer<uint>(endianness)),
        TypeCode.UInt64 => new EnumBinarySerializer<TEnum, ulong>(
            new IntegerBinarySerializer<ulong>(endianness)),
        TypeCode.Int64 => new EnumBinarySerializer<TEnum, long>(
            new IntegerBinarySerializer<long>(endianness)),
        TypeCode.Int16 => new EnumBinarySerializer<TEnum, short>(
            new IntegerBinarySerializer<short>(endianness)),
        TypeCode.UInt16 => new EnumBinarySerializer<TEnum, ushort>(
            new IntegerBinarySerializer<ushort>(endianness)),
        TypeCode.Byte => new EnumBinarySerializer<TEnum, byte>(
            new IntegerBinarySerializer<byte>(endianness)),
        TypeCode.SByte => new EnumBinarySerializer<TEnum, sbyte>(
            new IntegerBinarySerializer<sbyte>(endianness)),
        _ => throw new InvalidTypeArgumentException<TEnum>(),
    };

    public IBinarySerializer<TEnum> GetBaseSerializer() => serializer;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Deserialize(ReadOnlySpan<byte> data, ref TEnum value) => serializer.Deserialize(data, ref value);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Serialize(in TEnum data, Span<byte> buffer) => serializer.Serialize(in data, buffer);
}
