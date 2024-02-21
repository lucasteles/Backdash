using System.Diagnostics.CodeAnalysis;
using System.Net;
using Backdash;
using Backdash.Backends;
using Backdash.Core;
using ConsoleSession;

// ReSharper disable AccessToDisposedClosure

var frameDuration = FrameDuration.InTimeSpan(1);
using CancellationTokenSource cts = new();

// stops the game with ctr+c
Console.CancelKeyPress += (_, eventArgs) =>
{
    if (cts.IsCancellationRequested) return;
    eventArgs.Cancel = true;
    cts.Cancel();
    Console.WriteLine("Stopping...");
};

// parse console arguments
var (port, players) = ParseArgs(args);

// setup the rollback network session
var session = CreateSession(port, players);
// create the actual game
Game game = new(session);
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

// var localPlayer = players.Single(x => x.IsLocal());
// var remotePlayer = players.Single(x => x.IsRemote());

// -------------------------------------------------------------- //
//    Create and configure a game session for 2 Players           //
// -------------------------------------------------------------- //
static IRollbackSession<GameInput, GameState> CreateSession(
    int port,
    Player[] players
)
{
    var networkDelay = TimeSpan.Zero;
    //  var networkDelay =  player2.Type is PlayerType.Local ? Frames.ToTimeSpan(6) : default;

    var localPlayer = players.Single(x => x.IsLocal());

    var session = Rollback.CreateSession<GameInput, GameState>(
        new(port)
        {
            FrameDelay = 2,
            Log = new()
            {
                EnabledLevel = LogLevel.Off,
                // RunAsync = true,
            },
            Protocol = new()
            {
                NumberOfSyncPackets = 10,
                LogNetworkStats = false,
                NetworkDelay = networkDelay,
            },
        },
        // stateSerializer: new MyStateSerializer(),
        // logWriter: new TraceLogWriter(),
        logWriter: new FileLogWriter($"log_player_{localPlayer.Number}.txt")
    );

    session.AddPlayers(players);

    return session;
}

static (int Port, Player[] Players) ParseArgs(string[] args)
{
    if (args is not [{ } portArg, { } peer1Arg, { } peer2Arg]
        || !int.TryParse(portArg, out var port)
        || !TryParsePlayer(1, peer1Arg, out var player1)
        || !TryParsePlayer(2, peer2Arg, out var player2)
       )
        throw new InvalidOperationException("Invalid arguments...");

    return (port, [player1, player2]);
}

static bool TryParsePlayer(int number, string address,
    [NotNullWhen(true)] out Player? player)
{
    if (address.Equals("local", StringComparison.OrdinalIgnoreCase))
    {
        player = new LocalPlayer(number);
        return true;
    }

    if (IPEndPoint.TryParse(address, out var endPoint))
    {
        player = new RemotePlayer(number, endPoint);
        return true;
    }

    player = null;
    return false;
}