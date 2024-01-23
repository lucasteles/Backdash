using nGGPO.Network.Messages;

namespace nGGPO.Tests.Messages;

static class AssertSerialization
{
    public static bool Validate<T>(ref T value) where T : struct, IBinarySerializable, IEquatable<T>
    {
        using BinarySerializerFixture fixture = new();
        value.Serialize(fixture.Writer);
        T result = new();
        result.Deserialize(fixture.Reader);
        return result.Equals(value);
    }

    public static bool Offset<T>(ref T value) where T : struct, IBinarySerializable, IEquatable<T>
    {
        using BinarySerializerFixture fixture = new();
        value.Serialize(fixture.Writer);
        T result = new();
        result.Deserialize(fixture.Reader);
        return fixture.ReadOffset == fixture.WriteOffset;
    }
}
