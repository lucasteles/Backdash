using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Reflection;
using Backdash.Core;
using Backdash.Network;

namespace Backdash.Serialization.Internal;

static class BinarySerializerFactory
{
    public static IBinarySerializer<TInput> ForInteger<TInput>(Endianness? endianness = null)
        where TInput : unmanaged, IBinaryInteger<TInput>, IMinMaxValue<TInput> =>
        IntegerBinarySerializer.Create<TInput>(endianness);

    public static IBinarySerializer<TInput> ForInteger<TInput>(bool networkEndianness)
        where TInput : unmanaged, IBinaryInteger<TInput>, IMinMaxValue<TInput> =>
        ForInteger<TInput>(Platform.GetEndianness(networkEndianness));

    public static IBinarySerializer<TInput> ForEnum<TInput>(Endianness? endianness = null)
        where TInput : unmanaged, Enum =>
        EnumBinarySerializer.Create<TInput>(endianness);

    public static IBinarySerializer<TInput> ForEnum<TInput>(bool networkEndianness)
        where TInput : unmanaged, Enum =>
        ForEnum<TInput>(Platform.GetEndianness(networkEndianness));

    public static IBinarySerializer<TInput> ForStruct<TInput>() where TInput : unmanaged
    {
        var inputType = typeof(TInput);
        if (inputType.IsPrimitive || inputType.IsEnum)
            throw new ArgumentException("Non-primitive struct expected");

        return new StructBinarySerializer<TInput>();
    }

#if !NET9_0_OR_GREATER
    [RequiresDynamicCode("The native code for this instantiation might not be available at runtime.")]
#endif
    public static IBinarySerializer<TInput>? Get<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TInput>(bool networkEndianness = true)
        where TInput : unmanaged
    {
        var inputType = typeof(TInput);
        Type[] integerInterfaces = [typeof(IBinaryInteger<>), typeof(IMinMaxValue<>)];
        return inputType switch
        {
            { IsEnum: true }
                => typeof(BinarySerializerFactory)
                    .GetMethod(nameof(ForEnum), BindingFlags.Static | BindingFlags.Public,
                        null, CallingConventions.Any, [typeof(bool)], null)
                    ?.MakeGenericMethod(inputType)
                    .Invoke(null, [networkEndianness]) as IBinarySerializer<TInput>,
            { IsValueType: true } when !integerInterfaces.Except(inputType.GetInterfaces().Where(i => i.IsGenericType)
                    .Select(i => i.GetGenericTypeDefinition())).Any()
                => typeof(BinarySerializerFactory)
                    .GetMethod(nameof(ForInteger), BindingFlags.Static | BindingFlags.Public,
                        null, CallingConventions.Any, [typeof(bool)], null)
                    ?.MakeGenericMethod(inputType)
                    .Invoke(null, [networkEndianness]) as IBinarySerializer<TInput>,
            { IsValueType: true } when Mem.IsUnmanagedStruct<TInput>()
                => typeof(BinarySerializerFactory)
                    .GetMethod(nameof(ForStruct), BindingFlags.Static | BindingFlags.Public)
                    ?.MakeGenericMethod(inputType)
                    .Invoke(null, []) as IBinarySerializer<TInput>,
            _ => null,
        };
    }

#if !NET9_0_OR_GREATER
    [RequiresDynamicCode("The native code for this instantiation might not be available at runtime.")]
#endif
    public static IBinarySerializer<TInput> FindOrThrow<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TInput>(bool networkEndianness = true)
        where TInput : unmanaged =>
        Get<TInput>(networkEndianness)
        ?? throw new InvalidOperationException($"Unable to infer serializer for type {typeof(TInput).FullName}");
}
