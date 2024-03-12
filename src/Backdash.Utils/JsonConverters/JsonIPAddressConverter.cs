using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Backdash.JsonConverters;

/// <summary>
/// Json converter for IPAddress
/// </summary>
public sealed class JsonIPAddressConverter : JsonConverter<IPAddress>
{
    public const int MaxIPv4StringLength = 15;
    public const int MaxIPv6StringLength = 65;

    /// <inheritdoc />
    public override IPAddress Read(ref Utf8JsonReader reader,
        Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException(
                $"The JSON value could not be converted to {typeof(IPAddress)}");

        Span<char> charData = stackalloc char[MaxIPv6StringLength];
        var count = Encoding.UTF8.GetChars(
            reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan,
            charData);

        return IPAddress.TryParse(charData[..count], out var value)
            ? value
            : throw new JsonException(
                $"The JSON value could not be converted to {typeof(IPAddress)}");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, IPAddress value,
        JsonSerializerOptions options)
    {
        var data = value.AddressFamily is AddressFamily.InterNetwork
            ? stackalloc char[MaxIPv4StringLength]
            : stackalloc char[MaxIPv6StringLength];

        if (!value.TryFormat(data, out var charsWritten))
            throw new JsonException($"IPAddress [{value}] could not be written to JSON.");
        writer.WriteStringValue(data[..charsWritten]);
    }
}
