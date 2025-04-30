// ReSharper disable AccessToDisposedClosure, UnusedVariable

using System.Net;
using System.Net.Sockets;
using Backdash;
using Backdash.Core;
using Backdash.Network.Client;
using ConsoleGame;

var frameDuration = FrameTime.RateStep(60);
using CancellationTokenSource cts = new();

// stops the game with ctr+c
Console.CancelKeyPress += (_, eventArgs) =>
{
    if (cts.IsCancellationRequested) return;
    eventArgs.Cancel = true;
    cts.Cancel();
    Console.WriteLine("Stopping...");
};
// port and players
if (args is not [{ } portArg, { } playerCountArg, .. { } endpoints]
    || !int.TryParse(portArg, out var port)
    || !int.TryParse(playerCountArg, out var playerCount)
   )
    throw new InvalidOperationException("Invalid port argument");

// ## Netcode Configuration

// create rollback session builder
var builder = RollbackNetcode
    .WithInputType<GameInput>()
    .WithPort(port)
    .WithPlayerCount(playerCount)
    .WithInputDelayFrames(2)
    .WithLogLevel(LogLevel.Information)
    .UsePlugin<PluginSample>()
    .WithPackageStats()
    .ConfigureProtocol(options =>
    {
        options.NumberOfSyncRoundTrips = 10;
        // p.NetworkLatency = TimeSpan.FromMilliseconds(300);
        // p.DelayStrategy = Backdash.Network.DelayStrategy.Constant;
        // options.DisconnectTimeoutEnabled = false;
    });

// parse console arguments checking if it is a spectator
if (endpoints is ["spectate", { } hostArg] && IPEndPoint.TryParse(hostArg, out var host))
{
    builder
        .WithFileLogWriter($"logs/log_game_spectator_{port}.log", append: false)
        .ConfigureSpectator(options =>
        {
            options.HostEndPoint = host;
        });
}
// not a spectator, creating a `remote` game session
else
{
    var players = ParsePlayers(endpoints);
    var localPlayer = players.SingleOrDefault(x => x.IsLocal())
                      ?? throw new InvalidOperationException("No local player defined");
    builder
        // Write logs in a file with player number
        .WithFileLogWriter($"logs/log_game_player_{port}.log", append: false)
        .WithPlayers(players)
        .ForRemote();
}

var session = builder.Build();

// create the actual game
Game game = new(session, cts);

// set the session callbacks (like save state, load state, network events, etc..)
session.SetHandler(game);

// start background worker, like network IO, async messaging
session.Start(cts.Token);

try
{
    // kinda run a game-loop using a timer
    using PeriodicTimer timer = new(frameDuration);
    do game.Update();
    while (await timer.WaitForNextTickAsync(cts.Token));
}
catch (OperationCanceledException)
{
    // skip
}

// finishing the session
session.Dispose();
await session.WaitToStop();
Console.Clear();

return;

static NetcodePlayer[] ParsePlayers(IEnumerable<string> endpoints)
{
    var players = endpoints.Select(ParsePlayer).ToArray();

    if (!players.Any(x => x.IsLocal()))
        throw new InvalidOperationException("No defined local player");

    return players;
}

static NetcodePlayer ParsePlayer(string address)
{
    if (address.Equals("local", StringComparison.OrdinalIgnoreCase))
        return NetcodePlayer.CreateLocal();

    if (address.StartsWith("s:", StringComparison.OrdinalIgnoreCase))
        if (IPEndPoint.TryParse(address[2..], out var hostEndPoint))
            return NetcodePlayer.CreateSpectator(hostEndPoint);
        else
            throw new InvalidOperationException("Invalid spectator endpoint");

    if (IPEndPoint.TryParse(address, out var endPoint))
    {
        return NetcodePlayer.CreateRemote(endPoint);
    }

    throw new InvalidOperationException($"Invalid player argument: {address}");
}
