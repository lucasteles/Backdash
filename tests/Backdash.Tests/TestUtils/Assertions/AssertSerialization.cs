using Backdash.Serialization;
using Backdash.Tests.TestUtils.Fixtures;

namespace Backdash.Tests.TestUtils.Assertions;

static class AssertSerialization
{
    public delegate void SerializeFn<T>(ref T value, BinaryRawBufferWriter writer);

    public delegate void DeserializeFn<T>(ref T value, BinaryBufferReader reader);

    public static bool Validate<T>(ref T value, SerializeFn<T> serialize, DeserializeFn<T> deserialize)
        where T : struct, IEquatable<T>
    {
        using BinarySerializerFixture fixture = new();

        serialize(ref value, fixture.Writer);
        T result = new();
        deserialize(ref result, fixture.Reader);

        Assert.True(fixture.ReadOffset == fixture.WriteOffset);

        return result.Equals(value);
    }
}
