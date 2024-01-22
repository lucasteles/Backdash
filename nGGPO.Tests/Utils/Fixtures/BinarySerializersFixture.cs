namespace nGGPO.Tests;

[Serializable]
public sealed class BinarySerializersFixture
{
    public BinarySerializersFixture() =>
        BinarySerializerFactory.Register(new StringBinarySerializer());
}
