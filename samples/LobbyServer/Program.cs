using System.Net;
using System.Text.Json.Serialization;
using LobbyServer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Caching.Memory;
using static Microsoft.AspNetCore.Http.TypedResults;

var builder = WebApplication.CreateBuilder(args);
builder.Services
    .AddEndpointsApiExplorer()
    .ConfigureHttpJsonOptions(options => options
        .SerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(o => o // For Swagger
        .JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .AddSwaggerGen(options => options.SupportNonNullableReferenceTypes())
    .AddMemoryCache()
    .AddSingleton(TimeProvider.System);

var app = builder.Build();
app.UseSwagger().UseSwaggerUI();

app.MapGet("lobby/{name}", Results<Ok<Lobby>, NotFound> (IMemoryCache cache, string name) =>
    cache.Get<Lobby>(name.ToLower()) is { } lobby ? Ok(lobby) : NotFound());

app.MapPost("lobby", Results<Ok<LobbyResponse>, BadRequest, UnprocessableEntity> (
    HttpContext context, TimeProvider time, IMemoryCache cache, LobbyRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.LobbyName)
        || req.Port <= IPEndPoint.MinPort
        || req.Port > IPEndPoint.MaxPort
        || context.Connection.RemoteIpAddress is not { } userIp)
        return BadRequest();

    userIp.MapToIPv4();
    var name = req.LobbyName.Trim().ToLower();
    var lobby = cache.GetOrCreate(name, e =>
    {
        e.SetSlidingExpiration(Lobby.DefaultExpiration);
        return new Lobby(name, time.GetUtcNow());
    });
    if (lobby is null || lobby.Ready) return UnprocessableEntity();

    if (lobby.FindPeer(req.UserName) is { } existingPeer)
        return Ok(new LobbyResponse(lobby, existingPeer.Token));

    PeerEndpoint endpoint = new(userIp.ToString(), req.Port);
    Peer peer = new(req.UserName, endpoint);
    LobbyEntry entry = new(peer, req.Mode);
    lobby.AddPeer(entry);

    return Ok(new LobbyResponse(lobby, entry.Token));
});

app.MapPut("lobby/{id}/ready/{token}", Results<NoContent, NotFound, BadRequest> (
    IMemoryCache cache, string id, Guid token
) =>
{
    if (string.IsNullOrWhiteSpace(id)) return BadRequest();
    if (cache.Get<Lobby>(id.ToLower()) is not { } lobby || lobby.GetPlayer(token) is not { } player)
        return NotFound();

    player.ToggleReady();
    return NoContent();
});

app.Run();
