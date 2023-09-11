using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using Backdash.Core;

namespace Backdash.Serialization;

static class BinarySerializerFactory
{
    const bool DefaultEndianness = true;

    public static IBinarySerializer<TInput> ForPrimitive<TInput>(bool useEndianness = DefaultEndianness)
        where TInput : unmanaged
    {
        if (typeof(TInput).IsEnum)
            throw new InvalidOperationException(
                "Invalid Serializer: Use BinarySerializerFactory.ForEnum for enum types");

        return new PrimitiveBinarySerializer<TInput>(useEndianness);
    }

    public static IBinarySerializer<TInput> ForEnum<TInput, TUnderType>(bool useEndianness = DefaultEndianness)
        where TInput : unmanaged, Enum
        where TUnderType : unmanaged, IBinaryInteger<TUnderType> =>
        new EnumSerializer<TInput, TUnderType>(useEndianness);

    public static IBinarySerializer<TInput> ForEnum<TInput>(bool network = false)
        where TInput : unmanaged, Enum =>
        Type.GetTypeCode(typeof(TInput)) switch
        {
            TypeCode.Int32 => ForEnum<TInput, int>(network),
            TypeCode.UInt32 => ForEnum<TInput, uint>(network),
            TypeCode.UInt64 => ForEnum<TInput, ulong>(network),
            TypeCode.Int64 => ForEnum<TInput, long>(network),
            TypeCode.Int16 => ForEnum<TInput, short>(network),
            TypeCode.UInt16 => ForEnum<TInput, ushort>(network),
            TypeCode.Byte => ForEnum<TInput, byte>(network),
            TypeCode.SByte => ForEnum<TInput, sbyte>(network),
            _ => throw new InvalidTypeArgumentException<TInput>(),
        };

    public static IBinarySerializer<TInput> ForStruct<TInput>(
        bool marshall = false
    )
        where TInput : struct
    {
        var inputType = typeof(TInput);

        if (inputType.IsAutoLayout)
            throw new ArgumentException("Struct layout can't be auto");

        if (inputType.IsPrimitive || inputType.IsEnum)
            throw new ArgumentException("Struct input expected");

        if (inputType is { IsLayoutSequential: false, IsExplicitLayout: false })
            throw new ArgumentException("Input struct should have explicit or sequential layout ");

        return marshall
            ? new StructMarshalBinarySerializer<TInput>()
            : new StructBinarySerializer<TInput>();
    }

    public static IBinarySerializer<TInput>? Get<TInput>(bool enableEndianness = DefaultEndianness)
        where TInput : struct
    {
        var inputType = typeof(TInput);

        return inputType switch
        {
            { IsEnum: true } => typeof(BinarySerializerFactory)
                .GetMethod(nameof(ForEnum),
                    genericParameterCount: 2,
                    BindingFlags.Static | BindingFlags.Public,
                    null, CallingConventions.Any, Type.EmptyTypes, null
                )?
                .MakeGenericMethod(inputType, inputType.GetEnumUnderlyingType())
                .Invoke(null, [enableEndianness]) as IBinarySerializer<TInput>,

            { IsPrimitive: true } => typeof(BinarySerializerFactory)
                .GetMethod(nameof(ForPrimitive), BindingFlags.Static | BindingFlags.Public)?
                .MakeGenericMethod(inputType)
                .Invoke(null, [enableEndianness]) as IBinarySerializer<TInput>,

            { IsExplicitLayout: true } or { IsLayoutSequential: true } =>
                typeof(BinarySerializerFactory)
                    .GetMethod(nameof(ForStruct), BindingFlags.Static | BindingFlags.Public)?
                    .MakeGenericMethod(inputType)
                    .Invoke(null, [
                        Array.Exists(inputType.GetMembers(), m => Attribute.IsDefined(m, typeof(MarshalAsAttribute))),
                    ]) as IBinarySerializer<TInput>,

            _ => null,
        };
    }
}
