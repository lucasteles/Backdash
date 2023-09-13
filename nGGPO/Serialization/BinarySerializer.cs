namespace nGGPO.Serialization;

public interface IBinarySerializer
{
    ScopedBuffer Serialize<T>(T message) where T : notnull;
    T Deserialize<T>(byte[] body) where T : notnull;
}

public interface IBinarySerializer<T> where T : notnull
{
    int Size(T message);
    ScopedBuffer Serialize(T message);
    T Deserialize(byte[] body);
}
