// ReSharper disable AccessToDisposedClosure, UnusedVariable

using System.Diagnostics.CodeAnalysis;
using System.Net;
using Backdash;
using Backdash.Core;
using Backdash.Data;
using ConsoleGame;

var frameDuration = FrameSpan.GetDuration(1);
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
        .WithNetworkStats()
        .ConfigureProtocol(options =>
        {
            options.NumberOfSyncRoundtrips = 10;
            // p.LogNetworkStats = true;
            // p.NetworkLatency = TimeSpan.FromMilliseconds(300);
            // p.DelayStrategy = Backdash.Network.DelayStrategy.Constant;
        })
    ;

// parse console arguments checking if it is a spectator
if (endpoints is ["spectate", { } hostArg] && IPEndPoint.TryParse(hostArg, out var host))
{
    builder
        .WithFileLogWriter($"log_spectator_{port}.log", append: false)
        .ConfigureSpectator(options =>
        {
            options.HostEndPoint = host;
        });
}
// not a spectator, creating a `remote` game session
else
{
    var players = ParsePlayers(playerCount, endpoints);
    var localPlayer = players.SingleOrDefault(x => x.IsLocal())
                      ?? throw new InvalidOperationException("No local player defined");
    builder
        // Write logs in a file with player number
        .WithFileLogWriter($"log_player_{localPlayer.Number}.log", append: false)
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

static Player[] ParsePlayers(int totalNumberOfPlayers, IEnumerable<string> endpoints)
{
    var players = endpoints
        .Select((x, i) => TryParsePlayer(totalNumberOfPlayers, i + 1, x, out var player)
            ? player
            : throw new InvalidOperationException("Invalid endpoint address"))
        .ToArray();

    if (!players.Any(x => x.IsLocal()))
        throw new InvalidOperationException("No defined local player");

    return players;
}

static bool TryParsePlayer(
    int totalNumber,
    int number, string address,
    [NotNullWhen(true)] out Player? player)
{
    if (address.Equals("local", StringComparison.OrdinalIgnoreCase))
    {
        player = new LocalPlayer(number);
        return true;
    }

    if (IPEndPoint.TryParse(address, out var endPoint))
    {
        if (number <= totalNumber)
            player = new RemotePlayer(number, endPoint);
        else
            player = new Spectator(endPoint);
        return true;
    }

    player = null;
    return false;
}
