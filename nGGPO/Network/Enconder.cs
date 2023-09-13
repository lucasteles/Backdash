namespace nGGPO.Network;

public interface IBinaryEncoder
{
    ScopedBuffer Encode<T>(T message) where T : notnull;
    T Decode<T>(byte[] body) where T : notnull;
}

class StructBinaryEncoder : IBinaryEncoder
{
    public ScopedBuffer Encode<T>(T message) where T : notnull =>
        Mem.SerializeStruct(message);

    public T Decode<T>(byte[] body) where T : notnull =>
        Mem.DeserializeStruct<T>(body);
}

// public class JsonBinaryEncoder : IBinaryEncoder
// {
//     readonly JsonSerializerOptions options;
//
//     public JsonBinaryEncoder(JsonSerializerOptions options) => this.options = options;
//
//     public byte[] Encode<T>(T message) where T : notnull =>
//         JsonSerializer.SerializeToUtf8Bytes(message, options);
//
//     public void Return(in byte[] bytes)
//     {
//     }
//
//     public T Decode<T>(byte[] body) where T : notnull =>
//         JsonSerializer.Deserialize<T>(body, options)!;
// }