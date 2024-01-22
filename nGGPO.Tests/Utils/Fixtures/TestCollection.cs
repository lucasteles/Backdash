namespace nGGPO.Tests;

public static class TestCollection
{
    public const string Network = nameof(Network);
}

[CollectionDefinition(TestCollection.Network)]
public sealed class NetworkCollection : ICollectionFixture<BinarySerializersFixture>
{
}
