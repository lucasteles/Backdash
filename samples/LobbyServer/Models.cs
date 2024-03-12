using System.Net;

namespace LobbyServer;

using PeerToken = Guid;
using PeerId = Guid;

public enum PeerMode : byte
{
    Player,
    Spectator,
}

public sealed class Peer(string username, IPEndPoint endpoint)
{
    public PeerId PeerId { get; init; } = Guid.NewGuid();
    public string Username { get; } = username;
    public IPEndPoint Endpoint { get; } = endpoint;
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

    readonly List<LobbyEntry> entries = [];
    public readonly object Locker = new();

    public string Name { get; } = name;
    public PeerId Owner { get; private set; } = owner;
    public DateTimeOffset CreatedAt { get; } = createdAt;

    public DateTimeOffset ExpiresAt => CreatedAt + expiration;
    public bool Ready => Players.Count() > 1 && Players.All(p => p.Ready);

    public IEnumerable<Peer> Players
    {
        get
        {
            lock (Locker)
                return entries.Where(x => x.Mode is PeerMode.Player).Take(MaxPlayers)
                    .Select(x => x.Peer);
        }
    }

    public IEnumerable<Peer> Spectators
    {
        get
        {
            lock (Locker)
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
        lock (Locker)
        {
            if (Ready) return;
            if (Players.Count() >= MaxPlayers)
                entry = entry with { Mode = PeerMode.Spectator };

            entries.Add(entry);
        }
    }

    public void RemovePeer(LobbyEntry entry)
    {
        lock (Locker)
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
        AddPeer(entry with { Mode = mode });
    }

    public LobbyEntry? FindEntry(string username)
    {
        lock (Locker)
            return entries.Find(p =>
                p.Peer.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
    }

    public LobbyEntry? FindEntry(PeerToken token)
    {
        lock (Locker)
            return entries.Find(p => p.Token == token);
    }

    public bool IsEmpty()
    {
        lock (Locker)
            return entries.Count is 0;
    }

    public void Purge(DateTimeOffset now)
    {
        lock (Locker)
        {
            entries.RemoveAll(entry => now - entry.LastRead >= purgeTimeout);
            if (entries.Count > 0 && entries.TrueForAll(x => x.Peer.PeerId != Owner))
                Owner = entries.OrderByDescending(x => x.LastRead).First().Peer.PeerId;
        }
    }
}

public sealed record EnterLobbyRequest(string LobbyName, int Port, string Username, PeerMode Mode);

public sealed record EnterLobbyResponse(
    string Username,
    string LobbyName,
    PeerId PeerId,
    PeerToken Token,
    IPAddress IP
);
