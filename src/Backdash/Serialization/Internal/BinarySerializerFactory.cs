using System.Numerics;
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
}
