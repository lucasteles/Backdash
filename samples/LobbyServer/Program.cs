using System.Net;
using LobbyServer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using static Microsoft.AspNetCore.Http.TypedResults;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddOptions<AppSettings>().BindConfiguration("");

builder.Services
    .ConfigureHttpJsonOptions(options => JsonConfig.Options(options.SerializerOptions))
    .Configure<JsonOptions>(o => JsonConfig.Options(o.JsonSerializerOptions)) // For Swagger
    .AddEndpointsApiExplorer()
    .AddSwaggerGen(options =>
    {
        options.SupportNonNullableReferenceTypes();
        options.MapType<IPAddress>(() => new() {Type = "string"});
        options.MapType<IPEndPoint>(() => new() {Type = "string"});
    })
    .Configure<ForwardedHeadersOptions>(o => o.ForwardedHeaders = ForwardedHeaders.XForwardedFor)
    .AddMemoryCache()
    .AddSingleton(TimeProvider.System);

var app = builder.Build();
app.UseForwardedHeaders();
app.UseSwagger().UseSwaggerUI();

app.MapGet("info", (HttpContext context, TimeProvider time) => (object) new
{
    Date = time.GetLocalNow(),
    ClientIP = context.GetRemoteClientIP(),
    RemoteIP = context.Connection.RemoteIpAddress,
    RemoteIPv4 = context.Connection.RemoteIpAddress?.MapToIPv4(),
});

app.MapPost("lobby", Results<Ok<EnterLobbyResponse>, BadRequest, Conflict, UnprocessableEntity> (
    HttpContext context, TimeProvider time, IMemoryCache cache, IOptions<AppSettings> settings,
    EnterLobbyRequest req
) =>
{
    if (string.IsNullOrWhiteSpace(req.LobbyName)
        || req.LobbyName.Length > 40
        || req.Port <= IPEndPoint.MinPort
        || req.Port > IPEndPoint.MaxPort
        || context.GetRemoteClientIP() is not { } userIp)
        return BadRequest();

    var lobbyName = req.LobbyName.NormalizeName();
    var expiration = settings.Value.LobbyExpiration;
    var userName = req.Username.NormalizeName();
    var peerId = Guid.NewGuid();
    var now = time.GetUtcNow();

    var lobby = cache.GetOrCreate(lobbyName, e =>
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
        return UnprocessableEntity();

    lock (lobby.Locker)
    {
        var userNameIndex = 2;
        var nextUserName = userName;
        while (lobby.FindEntry(nextUserName) is not null)
            nextUserName = $"{userName}{userNameIndex++}";
        userName = nextUserName;

        IPEndPoint userEndpoint = new(userIp, req.Port);
        Peer peer = new(userName, userEndpoint)
        {
            PeerId = peerId,
        };

        LobbyEntry entry = new(peer, req.Mode)
        {
            LastRead = now,
        };

        lobby.AddPeer(entry);
        return Ok(new EnterLobbyResponse(
            userName, lobbyName, entry.Peer.PeerId, entry.Token, userIp));
    }
});

app.MapGet("lobby/{name}",
    Results<Ok<Lobby>, NotFound, UnauthorizedHttpResult> (
        IMemoryCache cache, TimeProvider time,
        [FromHeader] Guid token,
        string name
    ) =>
    {
        if (cache.Get<Lobby>(name.NormalizeName()) is not { } lobby) return NotFound();
        if (lobby.FindEntry(token) is not { } user) return Unauthorized();

        var now = time.GetUtcNow();
        user.LastRead = now;
        lobby.Purge(now);

        return Ok(lobby);
    });

app.MapDelete("lobby/{name}",
    Results<NoContent, NotFound, BadRequest, UnprocessableEntity, UnauthorizedHttpResult>
        (IMemoryCache cache, [FromHeader] Guid token, string name) =>
    {
        if (string.IsNullOrWhiteSpace(name)) return BadRequest();
        var lobbyName = name.NormalizeName();

        if (cache.Get<Lobby>(lobbyName) is not { } lobby) return NotFound();
        if (lobby.FindEntry(token) is not { } entry) return Unauthorized();
        if (lobby.Ready) return UnprocessableEntity();

        lobby.RemovePeer(entry);

        if (lobby.IsEmpty())
            cache.Remove(lobbyName);

        return NoContent();
    });

app.MapPut("lobby/{name}",
    Results<NoContent, NotFound, BadRequest, UnprocessableEntity, UnauthorizedHttpResult> (
        IMemoryCache cache, [FromHeader] Guid token, string name
    ) =>
    {
        if (string.IsNullOrWhiteSpace(name)) return BadRequest();
        var lobbyName = name.NormalizeName();

        if (cache.Get<Lobby>(lobbyName) is not { } lobby) return NotFound();
        if (lobby.FindEntry(token) is not { } entry) return Unauthorized();
        if (lobby.Ready) return UnprocessableEntity();

        if (entry.Mode is PeerMode.Player)
            lock (lobby.Locker)
                entry.Peer.ToggleReady();

        return NoContent();
    });

app.MapPut("lobby/{name}/mode/{mode}",
    Results<NoContent, NotFound, BadRequest, UnprocessableEntity, UnauthorizedHttpResult> (
        IMemoryCache cache,
        [FromHeader] Guid token,
        [FromRoute] string name,
        [FromRoute] PeerMode mode
    ) =>
    {
        if (string.IsNullOrWhiteSpace(name)) return BadRequest();
        var lobbyName = name.NormalizeName();

        if (cache.Get<Lobby>(lobbyName) is not { } lobby) return NotFound();
        if (lobby.FindEntry(token) is not { } entry) return Unauthorized();
        if (lobby.Ready) return UnprocessableEntity();

        lobby.ChangePeerMode(entry, mode);
        return NoContent();
    });

app.Run();

public record Foo(IPAddress Address, IPEndPoint EndPoint);
