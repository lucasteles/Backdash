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

    public static IBinarySerializer<TInput> ForInteger<TInput>(bool isUnsigned, Endianness? endianness = null)
        where TInput : unmanaged, IBinaryInteger<TInput> =>
        IntegerBinarySerializer.Create<TInput>(isUnsigned, endianness);

    public static IBinarySerializer<TInput> ForEnum<TInput>(Endianness? endianness = null)
        where TInput : unmanaged, Enum =>
        EnumBinarySerializer.Create<TInput>(endianness);

    public static IBinarySerializer<TInput> ForStruct<TInput>() where TInput : unmanaged
    {
        if (typeof(TInput).IsPrimitive || typeof(TInput).IsEnum)
            throw new ArgumentException("Non-primitive struct expected");

        return new StructBinarySerializer<TInput>();
    }

    public static IBinarySerializer<TInput>? Get<TInput>(bool networkEndianness = true)
        where TInput : unmanaged =>
        Get<TInput>(Platform.GetNetworkEndianness(networkEndianness));

    public static IBinarySerializer<TInput>? Get<TInput>(Endianness inputEndianness)
        where TInput : unmanaged
    {
#if AOT_ENABLED
        return null;
#else
        var inputType = typeof(TInput);
        Type[] integerInterfaces = [typeof(IBinaryInteger<>), typeof(IMinMaxValue<>)];
        return inputType switch
        {
            { IsEnum: true }
                => typeof(BinarySerializerFactory)
                    .GetMethod(nameof(ForEnum), BindingFlags.Static | BindingFlags.Public,
                        null, CallingConventions.Any, [typeof(Endianness?)], null)
                    ?.MakeGenericMethod(inputType)
                    .Invoke(null, [inputEndianness]) as IBinarySerializer<TInput>,
            { IsValueType: true } when !integerInterfaces.Except(inputType.GetInterfaces().Where(i => i.IsGenericType)
                    .Select(i => i.GetGenericTypeDefinition())).Any()
                => typeof(BinarySerializerFactory)
                    .GetMethod(nameof(ForInteger), BindingFlags.Static | BindingFlags.Public,
                        null, CallingConventions.Any, [typeof(Endianness?)], null)
                    ?.MakeGenericMethod(inputType)
                    .Invoke(null, [inputEndianness]) as IBinarySerializer<TInput>,
            { IsValueType: true } when Mem.IsUnmanagedStruct<TInput>()
                => typeof(BinarySerializerFactory)
                    .GetMethod(nameof(ForStruct), BindingFlags.Static | BindingFlags.Public)
                    ?.MakeGenericMethod(inputType)
                    .Invoke(null, []) as IBinarySerializer<TInput>,
            _ => null,
        };
#endif
    }

    public static IBinarySerializer<TInput> FindOrThrow<TInput>(Endianness inputEndianness)
        where TInput : unmanaged =>
        Get<TInput>(inputEndianness)
        ?? throw new InvalidOperationException($"Unable to infer serializer for type {typeof(TInput).FullName}");
}
