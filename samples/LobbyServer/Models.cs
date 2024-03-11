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
    public PeerId PeerId { get; init; } = Guid.NewGuid();
    public string Username { get; } = username;
    public PeerEndpoint Endpoint { get; } = endpoint;
    public bool Ready { get; private set; }
    public void ToggleReady() => Ready = !Ready;
}

public sealed record LobbyEntry(Peer Peer, PeerMode Mode)
{
    public PeerToken Token { get; init; } = PeerToken.NewGuid();
    public required DateTimeOffset LastRead { get; set; }
}

public sealed record SpectatorMapping(PeerId Host, IEnumerable<PeerId> Watchers);

public sealed class Lobby(
    string name,
    PeerId owner,
    TimeSpan expiration,
    TimeSpan purgeTimeout,
    DateTimeOffset createdAt
)
{
    const int MaxPlayers = 4;

    readonly object locker = new();

    readonly HashSet<LobbyEntry> entries = [];

    public string Name { get; } = name;
    public PeerId Owner { get; private set; } = owner;
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

    public void Purge(DateTimeOffset now)
    {
        lock (locker)
        {
            entries.RemoveWhere(entry => now - entry.LastRead > purgeTimeout);
            if (entries.Count > 0 && entries.All(x => x.Peer.PeerId != Owner))
            {
                Owner = entries.OrderByDescending(x => x.LastRead).First().Peer.PeerId;
            }
        }
    }
}

public sealed record EnterLobbyRequest(string LobbyName, int Port, string Username, PeerMode Mode);

public sealed record EnterLobbyResponse(
    string Username,
    string LobbyName,
    PeerId PeerId,
    PeerToken Token);
