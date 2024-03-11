namespace SpaceWar.Models;

public sealed class Peer
{
    public required Guid PeerId { get; init; }
    public required string Username { get; init; }
    public required PeerEndpoint Endpoint { get; init; }
    public bool Ready { get; init; }
}

public sealed record PeerEndpoint(string IP, int Port);
