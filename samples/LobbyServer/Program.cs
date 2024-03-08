using System.Net;
using System.Text.Json.Serialization;
using LobbyServer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using static Microsoft.AspNetCore.Http.TypedResults;

var builder = WebApplication.CreateBuilder(args);
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

app.MapGet("lobby/{name}", Results<Ok<Lobby>, NotFound> (IMemoryCache cache, string name) =>
    cache.Get<Lobby>(name.ToLower()) is { } lobby ? Ok(lobby) : NotFound());

app.MapPost("lobby", Results<Ok<EnterLobbyResponse>, BadRequest, Conflict, UnprocessableEntity> (
    HttpContext context, TimeProvider time, IMemoryCache cache, EnterLobbyRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.LobbyName)
        || req.LobbyName.Length > 40
        || req.Port <= IPEndPoint.MinPort
        || req.Port > IPEndPoint.MaxPort
        || context.Connection.RemoteIpAddress is not { } userIp)
        return BadRequest();

    var name = req.LobbyName.Trim().ToLower();
    var lobby = cache.GetOrCreate(name, e =>
    {
        e.SetSlidingExpiration(Lobby.DefaultExpiration);
        return new Lobby(name, time.GetUtcNow());
    });
    if (lobby is null || lobby.Ready) return UnprocessableEntity();

    PeerEndpoint endpoint = new(userIp.ToString(), req.Port);
    Peer peer = new(req.UserName, endpoint);

    if (lobby.FindEntry(req.UserName) is not null)
        return Conflict();

    LobbyEntry entry = new(peer, req.Mode);
    lobby.AddPeer(entry);
    return Ok(new EnterLobbyResponse(entry.Peer.PeerId, entry.Token));
});

app.MapDelete("lobby/{name}/{token}",
    Results<NoContent, NotFound, BadRequest, UnprocessableEntity>
        (IMemoryCache cache, string name, Guid token) =>
    {
        if (string.IsNullOrWhiteSpace(name)) return BadRequest();
        if (cache.Get<Lobby>(name.ToLower()) is not { } lobby ||
            lobby.FindEntry(token) is not { } entry)
            return NotFound();

        if (lobby.Ready) return UnprocessableEntity();

        lobby.RemovePeer(entry);
        return NoContent();
    });

app.MapPut("lobby/{name}/{token}/ready",
    Results<NoContent, NotFound, BadRequest, UnprocessableEntity> (
        IMemoryCache cache, string name, Guid token
    ) =>
    {
        if (string.IsNullOrWhiteSpace(name)) return BadRequest();
        if (cache.Get<Lobby>(name.ToLower()) is not { } lobby ||
            lobby.FindEntry(token) is not { } entry)
            return NotFound();

        if (lobby.Ready) return UnprocessableEntity();

        if (entry.Mode is PeerMode.Player)
            entry.Peer.ToggleReady();

        return NoContent();
    });

app.MapPut("lobby/{name}/{token}/mode/{mode}",
    Results<NoContent, NotFound, BadRequest, UnprocessableEntity> (
        IMemoryCache cache, string name, Guid token,
        [FromRoute] PeerMode mode
    ) =>
    {
        if (string.IsNullOrWhiteSpace(name)) return BadRequest();
        if (cache.Get<Lobby>(name.ToLower()) is not { } lobby ||
            lobby.FindEntry(token) is not { } entry)
            return NotFound();

        if (lobby.Ready) return UnprocessableEntity();

        lobby.ChangePeerMode(entry, mode);
        return NoContent();
    });

app.Run();
