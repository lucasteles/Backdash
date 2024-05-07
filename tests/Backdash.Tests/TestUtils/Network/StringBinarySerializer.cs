using System.Text;
using Backdash.Serialization;

namespace Backdash.Tests.TestUtils.Network;

public readonly record struct StringValue(string Value)
{
    public static implicit operator string(StringValue value) => value.Value;
    public static implicit operator StringValue(string value) => new(value);
}
class StringBinarySerializer : IBinarySerializer<StringValue>
{
    public int Serialize(in StringValue data, Span<byte> buffer) =>
        Encoding.UTF8.GetBytes(data.Value, buffer);
    public int Deserialize(ReadOnlySpan<byte> data, ref StringValue value)
    {
        value = Encoding.UTF8.GetString(data);
        return value.Value.Length;
    }
}
