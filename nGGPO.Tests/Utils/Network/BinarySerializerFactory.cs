using nGGPO.Serialization;

namespace nGGPO.Tests;

public static class BinarySerializerFactory
{
    static readonly Dictionary<Type, object> knownSerializers = [];

    public static IBinarySerializer<T> Create<T>() where T : struct
    {
        var type = typeof(T);

        if (knownSerializers.TryGetValue(type, out var objSerializer)
            && objSerializer is IBinarySerializer<T> serializer
           )
            return serializer;

        if (BinarySerializers.Get<T>() is { } baseSerializer)
            return baseSerializer;

        throw new InvalidOperationException($"Unable to find serializer for {type.Name}");
    }

    public static void Register<TMsg>(IBinarySerializer<TMsg> serializer) where TMsg : struct =>
        knownSerializers.Add(typeof(TMsg), serializer);
}
