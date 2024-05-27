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

// netcode configurations
RollbackOptions options = new()
{
    FrameDelay = 2,
    Log = new()
    {
        EnabledLevel = LogLevel.Information,
    },
    Protocol = new()
    {
        NumberOfSyncRoundtrips = 10,
        // LogNetworkStats = true,
        // NetworkDelay = TimeSpan.FromMilliseconds(300),
        // DelayStrategy = Backdash.Network.DelayStrategy.Constant,
    },
};

// Set up the rollback network session
IRollbackSession<GameInput, GameState> session;

// parse console arguments checking if it is a spectator
if (endpoints is ["spectate", { } hostArg] && IPEndPoint.TryParse(hostArg, out var host))
    session = RollbackNetcode.CreateSpectatorSession<GameInput, GameState>(
        port, host, playerCount, options, new()
        {
            LogWriter = new FileTextLogWriter($"log_spectator_{port}.log", append: false),
        }
    );
// not a spectator, creating a peer 2 peer game session
else
    session = CreatePlayerSession(port, options, ParsePlayers(playerCount, endpoints));

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

// -------------------------------------------------------------- //
//    Create and configure a game session                         //
// -------------------------------------------------------------- //
static IRollbackSession<GameInput, GameState> CreatePlayerSession(
    int port,
    RollbackOptions options,
    Player[] players
)
{
    var localPlayer = players.SingleOrDefault(x => x.IsLocal());
    if (localPlayer is null)
        throw new InvalidOperationException("No local player defined");
    // Write logs in a file with player number
    var fileLogWriter = new FileTextLogWriter($"log_player_{localPlayer.Number}.log", append: false);
    var session = RollbackNetcode.CreateSession<GameInput, GameState>(
        port,
        options,
        new()
        {
            LogWriter = fileLogWriter,
            // StateSerializer = new GameStateSerializer(),
        }
    );
    session.AddPlayers(players);
    return session;
}

static Player[] ParsePlayers(int totalNumberOfPlayers, IEnumerable<string> endpoints)
{
    var players = endpoints
        .Select((x, i) => TryParsePlayer(totalNumberOfPlayers, i + 1, x, out var player)
            ? player
            : throw new InvalidOperationException("Invalid endpoint address"))
        .ToArray();

    if (players.All(x => !x.IsLocal()))
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
