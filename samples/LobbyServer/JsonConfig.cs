using System.Text.Json;
using System.Text.Json.Serialization;
using Backdash.JsonConverters;

namespace LobbyServer;

public static class JsonConfig
{
    static readonly JsonConverter[] Converters =
    [
        new JsonStringEnumConverter(),
        new JsonIPAddressConverter(),
        new JsonIPEndPointConverter(),
    ];

    public static void Options(JsonSerializerOptions options)
    {
        foreach (var converter in Converters)
            options.Converters.Add(converter);
    }
}
