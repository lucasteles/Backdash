using System.Text.Json.Serialization;

namespace SpaceWar.Models;

public sealed class User
{
    [JsonPropertyName("PeerId")]
    public required Guid UserId { get; init; }

    public required Guid Token { get; init; }
    public required string Username { get; init; }
    public required string LobbyName { get; init; }
    public bool Ready { get; init; }
}
