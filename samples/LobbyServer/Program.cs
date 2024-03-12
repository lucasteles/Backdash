using System.Net;
using System.Text.Json.Serialization;
using LobbyServer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using static Microsoft.AspNetCore.Http.TypedResults;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
builder.Services
    .AddEndpointsApiExplorer()
    .ConfigureHttpJsonOptions(options => options
        .SerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .Configure<JsonOptions>(o => o // For Swagger
        .JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .AddSwaggerGen(options => options.SupportNonNullableReferenceTypes())
    .AddMemoryCache()
    .AddSingleton(TimeProvider.System);

var app = builder.Build();
app.UseSwagger().UseSwaggerUI();

app.MapGet("lobby/{name}",
    Results<Ok<Lobby>, NotFound, UnauthorizedHttpResult> (
        IMemoryCache cache, TimeProvider time,
        string name, [FromQuery] Guid token
    ) =>
    {
        if (cache.Get<Lobby>(name.ToLower()) is not { } lobby)
            return NotFound();

        if (lobby.FindEntry(token) is not { } user)
            return Unauthorized();

        var now = time.GetUtcNow();
        user.LastRead = now;
        lobby.Purge(now);

        return Ok(lobby);
    });

app.MapPost("lobby", Results<Ok<EnterLobbyResponse>, BadRequest, Conflict, UnprocessableEntity> (
    HttpContext context, TimeProvider time, IMemoryCache cache, IConfiguration configuration,
    EnterLobbyRequest req
) =>
{
    if (string.IsNullOrWhiteSpace(req.LobbyName)
        || req.LobbyName.Length > 40
        || req.Port <= IPEndPoint.MinPort
        || req.Port > IPEndPoint.MaxPort
        || context.Connection.RemoteIpAddress is not { } userIp)
        return BadRequest();

    var lobbyName = Normalize.Name(req.LobbyName);
    var expiration = configuration.GetValue<TimeSpan>("LobbyExpiration");
    var purgeTimeout = configuration.GetValue<TimeSpan>("PurgeTimeout");
    var userName = Normalize.Name(req.Username);
    var peerId = Guid.NewGuid();
    var now = time.GetUtcNow();

    var lobby = cache.GetOrCreate(lobbyName, e =>
    {
        e.SetSlidingExpiration(expiration);
        return new Lobby(
            name: lobbyName,
            owner: peerId,
            expiration: expiration,
            purgeTimeout: purgeTimeout,
            createdAt: now
        );
    });

    if (lobby is null || lobby.Ready)
        return UnprocessableEntity();

    lock (lobby.Locker)
    {
        var userNameIndex = 2;
        var nextUserName = userName;
        while (lobby.FindEntry(nextUserName) is not null)
            nextUserName = $"{userName}{userNameIndex++}";
        userName = nextUserName;

        PeerEndpoint userEndpoint = new(userIp.ToString(), req.Port);
        Peer peer = new(userName, userEndpoint)
        {
            PeerId = peerId,
        };

        LobbyEntry entry = new(peer, req.Mode)
        {
            LastRead = now,
        };

        lobby.AddPeer(entry);
        return Ok(new EnterLobbyResponse(userName, lobbyName, entry.Peer.PeerId, entry.Token));
    }
});

app.MapDelete("lobby/{name}",
    Results<NoContent, NotFound, BadRequest, UnprocessableEntity>
        (IMemoryCache cache, string name, [FromQuery] Guid token) =>
    {
        if (string.IsNullOrWhiteSpace(name)) return BadRequest();
        var lobbyName = Normalize.Name(name);
        if (cache.Get<Lobby>(lobbyName) is not { } lobby ||
            lobby.FindEntry(token) is not { } entry)
            return NotFound();

        if (lobby.Ready)
            return UnprocessableEntity();

        lobby.RemovePeer(entry);

        if (lobby.IsEmpty())
            cache.Remove(lobbyName);

        return NoContent();
    });

app.MapPut("lobby/{name}",
    Results<NoContent, NotFound, BadRequest, UnprocessableEntity> (
        IMemoryCache cache, string name, [FromQuery] Guid token
    ) =>
    {
        if (string.IsNullOrWhiteSpace(name)) return BadRequest();
        var lobbyName = Normalize.Name(name);
        if (cache.Get<Lobby>(lobbyName) is not { } lobby ||
            lobby.FindEntry(token) is not { } entry)
            return NotFound();

        if (lobby.Ready) return UnprocessableEntity();

        if (entry.Mode is PeerMode.Player)
            lock (lobby.Locker)
                entry.Peer.ToggleReady();

        return NoContent();
    });

app.MapPut("lobby/{name}/mode/{mode}",
    Results<NoContent, NotFound, BadRequest, UnprocessableEntity> (
        IMemoryCache cache, string name,
        [FromQuery] Guid token,
        [FromRoute] PeerMode mode
    ) =>
    {
        if (string.IsNullOrWhiteSpace(name)) return BadRequest();
        var lobbyName = Normalize.Name(name);
        if (cache.Get<Lobby>(lobbyName) is not { } lobby ||
            lobby.FindEntry(token) is not { } entry)
            return NotFound();

        if (lobby.Ready) return UnprocessableEntity();

        lobby.ChangePeerMode(entry, mode);
        return NoContent();
    });

app.Run();
