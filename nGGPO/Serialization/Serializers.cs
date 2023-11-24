using System;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using nGGPO.Network;
using nGGPO.Serialization.Buffer;
using nGGPO.Utils;

namespace nGGPO.Serialization;

sealed class StructMarshalBinarySerializer<T> : IBinarySerializer<T> where T : struct
{
    public T Deserialize(in ReadOnlySpan<byte> data) => Mem.UnmarshallStruct<T>(in data);

    public int Serialize(ref T data, Span<byte> buffer) => Mem.MarshallStruct(in data, in buffer);
}

sealed class StructBinarySerializer<T> : IBinarySerializer<T> where T : struct
{
    public T Deserialize(in ReadOnlySpan<byte> data) =>
        Mem.ReadStruct<T>(in data);

    public int Serialize(ref T data, Span<byte> buffer) =>
        Mem.WriteStruct(in data, buffer);
}

class UdpMsgBinarySerializer : IBinarySerializer<UdpMsg>
{
    public bool Network { get; init; }

    public UdpMsg Deserialize(in ReadOnlySpan<byte> data)
    {
        var offset = 0;
        NetworkBufferReader reader = new(data, ref offset) {Network = Network};

        var msg = new UdpMsg();
        msg.Deserialize(reader);
        return msg;
    }

    public int Serialize(ref UdpMsg data, Span<byte> buffer)
    {
        var offset = 0;
        NetworkBufferWriter writer = new(buffer, ref offset) {Network = Network};
        data.Serialize(writer);
        return offset;
    }
}

static class BinarySerializers
{
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
        static readonly PrimitiveBinarySerializer<TInt> ValueBinarySerializer = new();

        public TEnum Deserialize(in ReadOnlySpan<byte> data)
        {
            var underValue = ValueBinarySerializer.Deserialize(in data);
            return Mem.IntegerAsEnum<TEnum, TInt>(underValue);
        }

        public int Serialize(ref TEnum data, Span<byte> buffer)
        {
            var underValue = Mem.EnumAsInteger<TEnum, TInt>(data);
            return ValueBinarySerializer.SerializeScoped(ref underValue, buffer);
        }
    }

    public static IBinarySerializer<TInput> ForPrimitive<TInput>() where TInput : unmanaged =>
        new PrimitiveBinarySerializer<TInput>();

    public static IBinarySerializer<TInput> ForEnum<TInput, TUnderType>()
        where TInput : unmanaged, Enum
        where TUnderType : unmanaged, IBinaryInteger<TUnderType> =>
        new EnumSerializer<TInput, TUnderType>();

    public static IBinarySerializer<TInput> ForEnum<TInput>()
        where TInput : unmanaged, Enum => ForEnum<TInput, int>();

    public static IBinarySerializer<TInput> ForStructure<TInput>(bool marshall = false)
        where TInput : struct
    {
        var inputType = typeof(TInput);

        if (inputType.IsAutoLayout)
            throw new ArgumentException("Struct layout can't be auto");

        if (inputType.IsPrimitive || inputType.IsEnum)
            throw new ArgumentException("Struct input expected");

        if (inputType is {IsLayoutSequential: false, IsExplicitLayout: false})
            throw new ArgumentException("Input struct should have explicit or sequential layout ");

        return marshall
            ? new StructMarshalBinarySerializer<TInput>()
            : new StructBinarySerializer<TInput>();
    }

    public static IBinarySerializer<TInput>? Get<TInput>() where TInput : struct
    {
        var inputType = typeof(TInput);

        return inputType switch
        {
            {IsEnum: true} => typeof(BinarySerializers)
                .GetMethod(nameof(ForEnum),
                    genericParameterCount: 2,
                    BindingFlags.Static | BindingFlags.Public,
                    null, CallingConventions.Any, Type.EmptyTypes, null
                )?
                .MakeGenericMethod(inputType, inputType.GetEnumUnderlyingType())
                .Invoke(null, Array.Empty<object>()) as IBinarySerializer<TInput>,

            {IsPrimitive: true} => typeof(BinarySerializers)
                .GetMethod(nameof(ForPrimitive), BindingFlags.Static | BindingFlags.Public)?
                .MakeGenericMethod(inputType)
                .Invoke(null, Array.Empty<object>()) as IBinarySerializer<TInput>,

            {IsExplicitLayout: true} or {IsLayoutSequential: true} => typeof(BinarySerializers)
                .GetMethod(nameof(ForStructure), BindingFlags.Static | BindingFlags.Public)?
                .MakeGenericMethod(inputType)
                .Invoke(null, new object[]
                {
                    inputType.GetMembers()
                        .Any(m => Attribute.IsDefined(m, typeof(MarshalAsAttribute)))
                }) as IBinarySerializer<TInput>,

            _ => null,
        };
    }
}