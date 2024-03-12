using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Backdash.JsonConverters;

/// <summary>
/// Json converter for IPEndpoint
/// </summary>
public sealed class JsonIPEndPointConverter : JsonConverter<IPEndPoint>
{
    const int MaxIPv4StringLength = JsonIPAddressConverter.MaxIPv4StringLength + 6;
    const int MaxIPv6StringLength = JsonIPAddressConverter.MaxIPv6StringLength + 8;

    /// <inheritdoc />
    public override IPEndPoint Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException(
                $"The JSON value could not be converted to {typeof(IPEndPoint)}");

        Span<char> charData = stackalloc char[MaxIPv6StringLength];
        var count = Encoding.UTF8.GetChars(
            reader.HasValueSequence ? reader.ValueSequence.ToArray() : reader.ValueSpan,
            charData);

        return IPEndPoint.TryParse(charData[..count], out var value)
            ? value
            : throw new JsonException(
                $"The JSON value could not be converted to {typeof(IPEndPoint)}");
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, IPEndPoint value,
        JsonSerializerOptions options)
    {
        var isIpv6 = value.AddressFamily == AddressFamily.InterNetworkV6;
        var data = isIpv6
            ? stackalloc char[MaxIPv6StringLength]
            : stackalloc char[MaxIPv4StringLength];

        var offset = 0;
        if (isIpv6)
        {
            data[0] = '[';
            offset++;
        }

        if (!value.Address.TryFormat(data[offset..], out var addressCharsWritten))
            throw new JsonException($"IPEndPoint [{value}] could not be written to JSON.");

        offset += addressCharsWritten;
        if (isIpv6) data[offset++] = ']';

        data[offset++] = ':';
        if (!value.Port.TryFormat(data[offset..], out var portCharsWritten))
            throw new JsonException($"IPEndPoint [{value}] could not be written to JSON.");

        writer.WriteStringValue(data[..(offset + portCharsWritten)]);
    }
}
