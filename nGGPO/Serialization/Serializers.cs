using System;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using nGGPO.Network;
using nGGPO.Utils;

namespace nGGPO.Serialization;

public sealed class StructMarshalBinarySerializer<T> : IBinarySerializer<T> where T : struct
{
    public T Deserialize(in ReadOnlySpan<byte> data) =>
        Mem.UnmarshallStructure<T>(in data);

    public Span<byte> Serialize(in T data) => Mem.MarshallStructure(in data);
}

public sealed class StructBinarySerializer<T> : IBinarySerializer<T> where T : struct
{
    public T Deserialize(in ReadOnlySpan<byte> data) =>
        Mem.SpanAsStruct<T>(in data);

    public Span<byte> Serialize(in T data) => Mem.StructAsSpan(ref Unsafe.AsRef(in data));
}

public static class BinarySerializers
{
    sealed class PrimitiveBinarySerializer<T> : IBinarySerializer<T> where T : unmanaged
    {
        public bool Network { get; init; } = true;

        public readonly int Size = Unsafe.SizeOf<T>();

        public T Deserialize(in ReadOnlySpan<byte> data)
        {
            var value = Mem.ReadUnaligned<T>(data);
            return Network ? Endianness.TryNetworkToHostOrder(value) : value;
        }

        public Span<byte> Serialize(in T data) => SerializeScoped(ref Unsafe.AsRef(in data));

        public Span<byte> SerializeScoped(scoped ref T data)
        {
            var reordered = Network ? Endianness.TryHostToNetworkOrder(data) : data;
            return Mem.StructAsSpan(ref reordered);
        }
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

            if (Unsafe.SizeOf<TEnum>() != Unsafe.SizeOf<TInt>())
                throw new Exception("type mismatch");

            return Unsafe.As<TInt, TEnum>(ref underValue);
        }

        public Span<byte> Serialize(in TEnum data)
        {
            if (Unsafe.SizeOf<TEnum>() != Unsafe.SizeOf<TInt>())
                throw new Exception("type mismatch");

            var underValue = Unsafe.As<TEnum, TInt>(ref Unsafe.AsRef(in data));
            return ValueBinarySerializer.SerializeScoped(ref underValue);
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
            {IsPrimitive: true, IsEnum: true} => inputType
                .GetMethod(nameof(ForEnum))?
                .MakeGenericMethod(inputType, inputType.GetEnumUnderlyingType())
                .Invoke(null, Array.Empty<object>()) as IBinarySerializer<TInput>,

            {IsPrimitive: true} => inputType
                .GetMethod(nameof(ForPrimitive))?
                .MakeGenericMethod(inputType)
                .Invoke(null, Array.Empty<object>()) as IBinarySerializer<TInput>,

            {IsExplicitLayout: true} or {IsLayoutSequential: true} => inputType
                .GetMembers().Any(m => Attribute.IsDefined(m, typeof(MarshalAsAttribute)))
                ? new StructMarshalBinarySerializer<TInput>()
                : new StructBinarySerializer<TInput>(),

            _ => null,
        };
    }
}