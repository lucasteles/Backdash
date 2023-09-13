namespace nGGPO.Serialization.Serializers;

#if NET6_OR_GREATER
using System.Text.Json;

public class JsonBinarySerializer : IBinarySerializer
{
    readonly JsonSerializerOptions options;

    public JsonBinaryEncoder(JsonSerializerOptions options) => this.options = options;

    public ScopedBuffer Serialize<T>(T message) where T : notnull =>
        JsonSerializer.SerializeToUtf8Bytes(message, options);

    public T Deserialize<T>(byte[] body) where T : notnull =>
        JsonSerializer.Deserialize<T>(body, options)!;
}
#endif