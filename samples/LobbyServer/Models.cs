namespace LobbyServer;

using PeerToken = Guid;
using PeerId = Guid;

public enum PeerMode : byte
{
    Player,
    Spectator,
}

public sealed record PeerEndpoint(string IP, int Port);

public sealed class Peer(string username, PeerEndpoint endpoint)
{
    public PeerId PeerId { get; } = Guid.NewGuid();
    public string Username { get; } = username;

    public PeerEndpoint Endpoint { get; } = endpoint;
    public bool Ready { get; private set; }
    public void ToggleReady() => Ready = !Ready;
}

public sealed record LobbyEntry(Peer Peer, PeerMode Mode)
{
    public PeerToken Token { get; init; } = PeerToken.NewGuid();
}

public sealed record SpectatorMapping(PeerId Host, IEnumerable<PeerId> Watchers);

public sealed class Lobby(string name, TimeSpan expiration, DateTimeOffset createdAt)
{
    const int MaxPlayers = 4;

    readonly HashSet<LobbyEntry> entries = [];
    readonly object locker = new();

    public string Name { get; } = name;
    public DateTimeOffset CreatedAt { get; } = createdAt;
    public DateTimeOffset ExpiresAt => CreatedAt + expiration;

    public bool Ready => Players.Count() > 1 && Players.All(p => p.Ready);

    public IEnumerable<Peer> Players
    {
        get
        {
            lock (locker)
                return entries.Where(x => x.Mode is PeerMode.Player).Take(MaxPlayers)
                    .Select(x => x.Peer);
        }
    }

    public IEnumerable<Peer> Spectators
    {
        get
        {
            lock (locker)
                return entries.Where(x => x.Mode is PeerMode.Spectator).Select(x => x.Peer);
        }
    }

    public IEnumerable<SpectatorMapping> SpectatorMapping
    {
        get
        {
            if (!Ready) return [];
            return Players.Select((p, playerIndex) => new SpectatorMapping(
                p.PeerId,
                Spectators
                    .Select(x => x.PeerId)
                    .Where((_, specIndex) => specIndex % playerIndex is 0)
            ));
        }
    }

    public void AddPeer(LobbyEntry entry)
    {
        lock (locker)
        {
            if (Ready) return;
            if (Players.Count() >= MaxPlayers)
                entry = entry with {Mode = PeerMode.Spectator};

            entries.Add(entry);
        }
    }

    public void RemovePeer(LobbyEntry entry)
    {
        lock (locker)
        {
            if (Ready) return;
            entries.Remove(entry);
        }
    }

    public void ChangePeerMode(LobbyEntry entry, PeerMode mode)
    {
        if (Ready || entry.Mode == mode) return;
        RemovePeer(entry);
        if (entry.Peer.Ready) entry.Peer.ToggleReady();
        AddPeer(entry with {Mode = mode});
    }

    public LobbyEntry? FindEntry(string username)
    {
        lock (locker)
            return entries.FirstOrDefault(p =>
                p.Peer.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
    }

    public LobbyEntry? FindEntry(PeerToken token)
    {
        lock (locker)
            return entries.FirstOrDefault(p => p.Token == token);
    }

    public bool IsEmpty()
    {
        lock (locker)
            return entries.Count is 0;
    }
}

public sealed record EnterLobbyRequest(string LobbyName, int Port, string Username, PeerMode Mode);

public sealed record EnterLobbyResponse(string Username, PeerId PeerId, PeerToken Token);
