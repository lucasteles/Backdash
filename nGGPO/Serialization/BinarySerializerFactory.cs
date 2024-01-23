using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;

namespace nGGPO.Serialization;

static class BinarySerializerFactory
{
    public static IBinarySerializer<TInput> ForPrimitive<TInput>() where TInput : unmanaged
    {
        if (typeof(TInput).IsEnum)
            throw new InvalidOperationException(
                "Invalid Serializer: Use BinarySerializerFactory.ForEnum for enum types");

        return new PrimitiveBinarySerializer<TInput>();
    }

    public static IBinarySerializer<TInput> ForEnum<TInput, TUnderType>()
        where TInput : unmanaged, Enum
        where TUnderType : unmanaged, IBinaryInteger<TUnderType> =>
        new EnumSerializer<TInput, TUnderType>();

    public static IBinarySerializer<TInput> ForEnum<TInput>()
        where TInput : unmanaged, Enum =>
        Type.GetTypeCode(typeof(TInput)) switch
        {
            TypeCode.Int32 => ForEnum<TInput, int>(),
            TypeCode.UInt32 => ForEnum<TInput, uint>(),
            TypeCode.UInt64 => ForEnum<TInput, ulong>(),
            TypeCode.Int64 => ForEnum<TInput, long>(),
            TypeCode.Int16 => ForEnum<TInput, short>(),
            TypeCode.UInt16 => ForEnum<TInput, ushort>(),
            TypeCode.Byte => ForEnum<TInput, byte>(),
            TypeCode.SByte => ForEnum<TInput, sbyte>(),
            _ => throw new ArgumentOutOfRangeException(nameof(TInput)),
        };

    public static IBinarySerializer<TInput> ForStruct<TInput>(bool marshall = false)
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
            {IsEnum: true} => typeof(BinarySerializerFactory)
                .GetMethod(nameof(ForEnum),
                    genericParameterCount: 2,
                    BindingFlags.Static | BindingFlags.Public,
                    null, CallingConventions.Any, Type.EmptyTypes, null
                )?
                .MakeGenericMethod(inputType, inputType.GetEnumUnderlyingType())
                .Invoke(null, []) as IBinarySerializer<TInput>,

            {IsPrimitive: true} => typeof(BinarySerializerFactory)
                .GetMethod(nameof(ForPrimitive), BindingFlags.Static | BindingFlags.Public)?
                .MakeGenericMethod(inputType)
                .Invoke(null, []) as IBinarySerializer<TInput>,

            {IsExplicitLayout: true} or {IsLayoutSequential: true} =>
                typeof(BinarySerializerFactory)
                    .GetMethod(nameof(ForStruct), BindingFlags.Static | BindingFlags.Public)?
                    .MakeGenericMethod(inputType)
                    .Invoke(null, [
                        inputType.GetMembers()
                            .Any(m => Attribute.IsDefined(m, typeof(MarshalAsAttribute))),
                    ]) as IBinarySerializer<TInput>,

            _ => null,
        };
    }
}
