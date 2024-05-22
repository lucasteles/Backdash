using System.Numerics;
using Backdash.Core;
using Backdash.Network;

#if !AOT_ENABLED
using System.Reflection;
using System.Runtime.InteropServices;
#endif

namespace Backdash.Serialization;

static class BinarySerializerFactory
{
    public static IBinarySerializer<TInput> ForInteger<TInput>(bool networkEndianness = true)
        where TInput : unmanaged, IBinaryInteger<TInput>, IMinMaxValue<TInput>
    {
        var mode = Platform.GetEndianness(networkEndianness);
        return new IntegerBinarySerializer<TInput>(mode);
    }

    public static IBinarySerializer<TInput> ForEnum<TInput>(bool networkEndianness = true)
        where TInput : unmanaged, Enum
    {
        var mode = Platform.GetEndianness(networkEndianness);
        return new EnumBinarySerializer<TInput>(mode);
    }

    public static IBinarySerializer<TInput> ForStruct<TInput>(bool marshall = false)
        where TInput : struct
    {
        var inputType = typeof(TInput);
        if (inputType.IsAutoLayout)
            throw new ArgumentException("Struct layout can't be auto");
        if (inputType.IsPrimitive || inputType.IsEnum)
            throw new ArgumentException("Struct input expected");
        if (inputType is { IsLayoutSequential: false, IsExplicitLayout: false })
            throw new ArgumentException("Input struct should have explicit or sequential layout ");
#if AOT_ENABLED
        if (marshall)
            throw new InvalidOperationException("Marshalling is not valid on AOT");

        ThrowHelpers.ThrowIfTypeIsReferenceOrContainsReferences<TInput>();
        return new StructBinarySerializer<TInput>();
#else
        if (!marshall)
            ThrowHelpers.ThrowIfTypeIsReferenceOrContainsReferences<TInput>();
        return marshall
            ? new StructMarshalBinarySerializer<TInput>()
            : new StructBinarySerializer<TInput>();
#endif
    }

    public static IBinarySerializer<TInput>? Get<TInput>(bool networkEndianness = true)
        where TInput : unmanaged
    {
#if AOT_ENABLED
        return null;
#else
        var inputType = typeof(TInput);
        Type[] integerInterfaces = [typeof(IBinaryInteger<>), typeof(IMinMaxValue<>)];
        return inputType switch
        {
            { IsEnum: true } => typeof(BinarySerializerFactory)
                .GetMethod(nameof(ForEnum),
                    BindingFlags.Static | BindingFlags.Public, null,
                    CallingConventions.Any,
                    [typeof(bool)],
                    null
                )?
                .MakeGenericMethod(inputType)
                .Invoke(null, [networkEndianness]) as IBinarySerializer<TInput>,
            { IsValueType: true, }
                when !integerInterfaces.Except(inputType.GetInterfaces().Where(i => i.IsGenericType)
                    .Select(i => i.GetGenericTypeDefinition())).Any()
                => typeof(BinarySerializerFactory)
                    .GetMethod(nameof(ForInteger), BindingFlags.Static | BindingFlags.Public)?
                    .MakeGenericMethod(inputType)
                    .Invoke(null, [networkEndianness]) as IBinarySerializer<TInput>,
            { IsExplicitLayout: true } or { IsLayoutSequential: true }
                when Array.Exists(inputType.GetMembers(), m => Attribute.IsDefined(m, typeof(MarshalAsAttribute)))
                => typeof(BinarySerializerFactory)
                    .GetMethod(nameof(ForStruct), BindingFlags.Static | BindingFlags.Public)?
                    .MakeGenericMethod(inputType)
                    .Invoke(null, [true]) as IBinarySerializer<TInput>,
            { IsValueType: true }
                when !Mem.IsReferenceOrContainsReferences<TInput>()
                => typeof(BinarySerializerFactory)
                    .GetMethod(nameof(ForStruct), BindingFlags.Static | BindingFlags.Public)?
                    .MakeGenericMethod(inputType)
                    .Invoke(null, [false]) as IBinarySerializer<TInput>,
            _ => null,
        };
#endif
    }

    public static IBinarySerializer<TInput> FindOrThrow<TInput>(bool networkEndianness = true)
        where TInput : unmanaged =>
        Get<TInput>(networkEndianness)
        ?? throw new InvalidOperationException($"Unable to infer serializer for type {typeof(TInput).FullName}");
}
