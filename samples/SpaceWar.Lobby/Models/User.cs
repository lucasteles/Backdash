using System.Net;

namespace SpaceWar.Models;

public sealed class User
{
    public required Guid PeerId { get; init; }
    public required Guid Token { get; init; }
    public required string Username { get; init; }
    public required string LobbyName { get; init; }
    public required IPAddress IP { get; init; }
}
