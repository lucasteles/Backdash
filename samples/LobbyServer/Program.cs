using System.Net;
using LobbyServer;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.TypedResults;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();
builder.Services.AddOptions<AppSettings>().BindConfiguration("");
builder.Services
    .ConfigureHttpJsonOptions(options => options.SerializerOptions.AddCustomConverters())
    .Configure<JsonOptions>(o => o.JsonSerializerOptions.AddCustomConverters()) // For Swagger
    .AddEndpointsApiExplorer()
    .AddSwaggerGen(options =>
    {
        options.SupportNonNullableReferenceTypes();
        options.MapType<IPAddress>(() => new() { Type = "string" });
        options.MapType<IPEndPoint>(() => new() { Type = "string" });
    })
    .Configure<ForwardedHeadersOptions>(o => o.ForwardedHeaders = ForwardedHeaders.XForwardedFor)
    .AddMemoryCache()
    .AddSingleton(TimeProvider.System)
    .AddSingleton<LobbyRepository>();

builder.Services.AddHostedService<UdpListenerService>();

var app = builder.Build();
Console.Title = app.Environment.ApplicationName;
app.UseForwardedHeaders();
app.UseSwagger().UseSwaggerUI();

app.MapGet("info", (HttpContext context, TimeProvider time) => (object)new
{
    Date = time.GetLocalNow(),
    ClientIP = context.GetRemoteClientIP(),
    RemoteIP = context.Connection.RemoteIpAddress,
    RemoteIPv4 = context.Connection.RemoteIpAddress?.MapToIPv4(),
});

app.MapPost("lobby", Results<Ok<EnterLobbyResponse>, BadRequest, Conflict, UnprocessableEntity> (
    HttpContext context, LobbyRepository repository, EnterLobbyRequest req
) =>
{
    if (string.IsNullOrWhiteSpace(req.LobbyName)
        || req.LobbyName.Length > 40
        || context.GetRemoteClientIP() is not { } userIp)
        return BadRequest();

    if (repository.EnterOrCreate(userIp, req) is not { } lobbyResponse)
        return UnprocessableEntity();

    return Ok(lobbyResponse);
});

app.MapGet("lobby/{name}",
    Results<Ok<Lobby>, NotFound, UnauthorizedHttpResult> (
        LobbyRepository repository, TimeProvider time,
        [FromHeader] Guid? token,
        string name
    ) =>
    {
        if (repository.FindLobby(name) is not { } lobby)
            return NotFound();

        if (token is not null)
        {
            if (repository.FindEntry(token.Value) is null) return NotFound();
            if (lobby.FindEntry(token.Value) is null) return Unauthorized();
        }

        lobby.Purge(time.GetUtcNow());
        if (lobby.IsEmpty())
            repository.Remove(lobby);

        return Ok(lobby);
    });

app.MapDelete("lobby/{name}",
    Results<NoContent, NotFound, BadRequest, UnprocessableEntity, UnauthorizedHttpResult> (
        LobbyRepository repository, [FromHeader] Guid token, string name
    ) =>
    {
        if (string.IsNullOrWhiteSpace(name)) return BadRequest();
        if (repository.FindEntry(token) is null || repository.FindLobby(name) is not { } lobby)
            return NotFound();
        if (lobby.FindEntry(token) is not { } entry) return Unauthorized();

        if (lobby.Ready) return UnprocessableEntity();

        lock (lobby.Locker)
        {
            lobby.RemovePeer(entry);

            if (lobby.IsEmpty())
                repository.Remove(lobby);
        }

        return NoContent();
    });

app.MapPut("lobby/{name}",
    Results<NoContent, NotFound, BadRequest, UnprocessableEntity, UnauthorizedHttpResult> (
        LobbyRepository repository, [FromHeader] Guid token, string name
    ) =>
    {
        if (string.IsNullOrWhiteSpace(name)) return BadRequest();
        if (repository.FindEntry(token) is null || repository.FindLobby(name) is not { } lobby)
            return NotFound();
        if (lobby.FindEntry(token) is not { } entry) return Unauthorized();
        if (lobby.Ready) return UnprocessableEntity();

        if (entry.Mode is PeerMode.Player)
            lock (lobby.Locker)
                entry.Peer.ToggleReady();

        return NoContent();
    });

app.MapPut("lobby/{name}/mode/{mode}",
    Results<NoContent, NotFound, BadRequest, UnprocessableEntity, UnauthorizedHttpResult> (
        LobbyRepository repository,
        [FromHeader] Guid token,
        [FromRoute] string name,
        [FromRoute] PeerMode mode
    ) =>
    {
        if (string.IsNullOrWhiteSpace(name)) return BadRequest();
        if (repository.FindEntry(token) is null || repository.FindLobby(name) is not { } lobby)
            return NotFound();
        if (lobby.FindEntry(token) is not { } entry) return Unauthorized();
        if (lobby.Ready) return UnprocessableEntity();

        lobby.ChangePeerMode(entry, mode);
        return NoContent();
    });

if (app.Environment.IsDevelopment())
    _ = Task.Run(async () =>
    {
        if (Console.ReadKey().Key is ConsoleKey.Escape)
            await app.StopAsync();
    });

await app.RunAsync();
