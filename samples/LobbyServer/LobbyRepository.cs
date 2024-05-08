using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace LobbyServer;

public sealed class LobbyRepository(
    IMemoryCache cache,
    TimeProvider time,
    IOptions<AppSettings> settings
)
{
    const string LobbyCachePrefix = "lobby";

    public EnterLobbyResponse? EnterOrCreate(IPAddress remote, EnterLobbyRequest req)
    {
        var lobbyName = req.LobbyName.NormalizeName();
        var lobbyKey = lobbyName.WithPrefix(LobbyCachePrefix);
        var userName = req.Username.NormalizeName();
        var expiration = settings.Value.LobbyExpiration;
        var peerId = Guid.NewGuid();
        var now = time.GetUtcNow();

        var lobby = cache.GetOrCreate(lobbyKey, e =>
        {
            e.SetSlidingExpiration(expiration);
            return new Lobby(
                name: lobbyName,
                owner: peerId,
                expiration: expiration,
                purgeTimeout: settings.Value.PurgeTimeout,
                createdAt: now
            );
        });

        if (lobby is null || lobby.Ready)
            return null;

        lock (lobby.Locker)
        {
            var userNameIndex = 2;
            var nextUserName = userName;
            while (lobby.FindEntry(nextUserName) is not null)
                nextUserName = $"{userName}{userNameIndex++}";
            userName = nextUserName;

            Peer peer = new(userName, remote)
            {
                PeerId = peerId,
                LocalEndpoint = req.LocalEndpoint,
            };

            LobbyEntry entry = new(peer, req.Mode)
            {
                LastRead = now,
            };

            using var playerEntry = cache.CreateEntry(entry.Token);
            playerEntry.Value = entry;
            playerEntry.SetSlidingExpiration(expiration);

            lobby.AddPeer(entry);

            return new(
                userName, lobbyName, entry.Peer.PeerId, entry.Token, remote);
        }
    }

    public Lobby? FindLobby(string name)
    {
        var key = name.NormalizeName().WithPrefix(LobbyCachePrefix);
        return cache.Get<Lobby>(key);
    }

    public LobbyEntry? FindEntry(Guid peerToken)
    {
        if (!cache.TryGetValue<LobbyEntry>(peerToken, out var entry) || entry is null)
            return null;

        var now = time.GetUtcNow();
        entry.LastRead = now;

        return entry;
    }

    public void Remove(Lobby lobby) => cache.Remove(lobby.Name.WithPrefix(LobbyCachePrefix));
}
