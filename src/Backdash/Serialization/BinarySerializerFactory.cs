using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using Backdash.Core;
using Backdash.Network;
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
        where TInput : unmanaged, Enum =>
        Type.GetTypeCode(typeof(TInput)) switch
        {
            TypeCode.Int32 => new EnumBinarySerializer<TInput, int>(ForInteger<int>(networkEndianness)),
            TypeCode.UInt32 => new EnumBinarySerializer<TInput, uint>(ForInteger<uint>(networkEndianness)),
            TypeCode.UInt64 => new EnumBinarySerializer<TInput, ulong>(ForInteger<ulong>(networkEndianness)),
            TypeCode.Int64 => new EnumBinarySerializer<TInput, long>(ForInteger<long>(networkEndianness)),
            TypeCode.Int16 => new EnumBinarySerializer<TInput, short>(ForInteger<short>(networkEndianness)),
            TypeCode.UInt16 => new EnumBinarySerializer<TInput, ushort>(ForInteger<ushort>(networkEndianness)),
            TypeCode.Byte => new EnumBinarySerializer<TInput, byte>(ForInteger<byte>(networkEndianness)),
            TypeCode.SByte => new EnumBinarySerializer<TInput, sbyte>(ForInteger<sbyte>(networkEndianness)),
            _ => throw new InvalidTypeArgumentException<TInput>(),
        };
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
        if (!marshall)
            ThrowHelpers.ThrowIfTypeIsReferenceOrContainsReferences<TInput>();
        return marshall
            ? new StructMarshalBinarySerializer<TInput>()
            : new StructBinarySerializer<TInput>();
    }
    public static IBinarySerializer<TInput>? Get<TInput>(bool networkEndianness = true)
        where TInput : struct
    {
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
    }
    public static IBinarySerializer<TInput> FindOrThrow<TInput>(bool networkEndianness = true)
        where TInput : struct =>
        Get<TInput>(networkEndianness)
        ?? throw new InvalidOperationException($"Unable to infer serializer for type {typeof(TInput).FullName}");
}
