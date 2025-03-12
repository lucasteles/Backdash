using System.Numerics;
using System.Runtime.CompilerServices;
using Backdash.Core;
using Backdash.Network;

namespace Backdash.Serialization.Internal;

sealed class IntegerBinaryBigEndianSerializer<T>(bool isUnsigned)
    : IBinarySerializer<T> where T : unmanaged, IBinaryInteger<T>
{
    public Endianness Endianness => Endianness.BigEndian;

    static readonly int tSize = Unsafe.SizeOf<T>();

    public int Serialize(in T data, Span<byte> buffer)
    {
        Unsafe.AsRef(in data).TryWriteBigEndian(buffer, out var size);
        return size;
    }

    public int Deserialize(ReadOnlySpan<byte> data, ref T value)
    {
        value = T.ReadBigEndian(data[..tSize], isUnsigned);
        return tSize;
    }
}

sealed class IntegerBinaryLittleEndianSerializer<T>(bool isUnsigned)
    : IBinarySerializer<T> where T : unmanaged, IBinaryInteger<T>
{
    public Endianness Endianness => Endianness.LittleEndian;

    static readonly int tSize = Unsafe.SizeOf<T>();

    public int Serialize(in T data, Span<byte> buffer)
    {
        Unsafe.AsRef(in data).TryWriteLittleEndian(buffer, out var size);
        return size;
    }

    public int Deserialize(ReadOnlySpan<byte> data, ref T value)
    {
        value = T.ReadLittleEndian(data[..tSize], isUnsigned);
        return tSize;
    }
}

static class IntegerBinarySerializer
{
    public static IBinarySerializer<TInput> Create<TInput>(Endianness? endianness = null)
        where TInput : unmanaged, IBinaryInteger<TInput>, IMinMaxValue<TInput>
        => Create<TInput>(Mem.IsUnsigned<TInput>(), endianness);

    public static IBinarySerializer<TInput> Create<TInput>(bool isUnsigned, Endianness? endianness = null)
        where TInput : unmanaged, IBinaryInteger<TInput>
    {
        endianness ??= Platform.Endianness;

        return endianness switch
        {
            Endianness.LittleEndian => new IntegerBinaryLittleEndianSerializer<TInput>(isUnsigned),
            Endianness.BigEndian => new IntegerBinaryBigEndianSerializer<TInput>(isUnsigned),
            _ => throw new ArgumentOutOfRangeException(nameof(endianness), endianness, "Invalid endianness."),
        };
    }
}

sealed class EnumBinarySerializer<TEnum, TInt>(IBinarySerializer<TInt> serializer) : IBinarySerializer<TEnum>
    where TEnum : unmanaged, Enum
    where TInt : unmanaged, IBinaryInteger<TInt>
{
    public Endianness Endianness => serializer.Endianness;

    public IBinarySerializer<TInt> GetBaseSerializer() => serializer;

    public int Serialize(in TEnum data, Span<byte> buffer)
    {
        ref var underValue = ref Unsafe.As<TEnum, TInt>(ref Unsafe.AsRef(in data));
        return serializer.Serialize(in underValue, buffer);
    }

    public int Deserialize(ReadOnlySpan<byte> data, ref TEnum value)
    {
        ref var underValue = ref Unsafe.As<TEnum, TInt>(ref value);
        return serializer.Deserialize(data, ref underValue);
    }
}

static class EnumBinarySerializer
{
    public static IBinarySerializer<TEnum> Create<TEnum>(Endianness? endianness = null) where TEnum : unmanaged, Enum
    {
        endianness ??= Platform.Endianness;

        return Type.GetTypeCode(typeof(TEnum)) switch
        {
            TypeCode.Int32 => new EnumBinarySerializer<TEnum, int>(
                IntegerBinarySerializer.Create<int>(false, endianness)),
            TypeCode.UInt32 => new EnumBinarySerializer<TEnum, uint>(
                IntegerBinarySerializer.Create<uint>(true, endianness)),
            TypeCode.UInt64 => new EnumBinarySerializer<TEnum, ulong>(
                IntegerBinarySerializer.Create<ulong>(true, endianness)),
            TypeCode.UInt16 => new EnumBinarySerializer<TEnum, ushort>(
                IntegerBinarySerializer.Create<ushort>(true, endianness)),
            TypeCode.Byte => new EnumBinarySerializer<TEnum, byte>(
                IntegerBinarySerializer.Create<byte>(true, endianness)),
            TypeCode.Int64 => new EnumBinarySerializer<TEnum, long>(
                IntegerBinarySerializer.Create<long>(false, endianness)),
            TypeCode.Int16 => new EnumBinarySerializer<TEnum, short>(
                IntegerBinarySerializer.Create<short>(false, endianness)),
            TypeCode.SByte => new EnumBinarySerializer<TEnum, sbyte>(
                IntegerBinarySerializer.Create<sbyte>(false, endianness)),
            _ => throw new InvalidTypeArgumentException<TEnum>(),
        };
    }
}
