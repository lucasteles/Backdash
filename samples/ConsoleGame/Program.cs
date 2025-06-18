// ReSharper disable AccessToDisposedClosure, UnusedVariable

using Backdash;
using Backdash.Core;
using ConsoleGame;

using CancellationTokenSource cts = new();
Args gameArgs = new();

// Stops the game with ctr+c
ConfigureCtrC(cts);

// Configure and create the netcode session
var session = CreateNetcodeSession(gameArgs);

// Create the actual game logic / netcode handler
Game game = new(session, cts);

// set the session callbacks (like save state, load state, network events, etc..)
session.SetHandler(game);

// start background worker, like network IO, async messaging
session.Start(cts.Token);

// timer based game loop
await game.Run(cts.Token);

// finishing the session
session.Dispose();
await session.WaitToStop();

return;

static void ConfigureCtrC(CancellationTokenSource cancellationTokenSource) =>
    Console.CancelKeyPress += (_, eventArgs) =>
    {
        if (cancellationTokenSource.IsCancellationRequested) return;
        eventArgs.Cancel = true;
        cancellationTokenSource.Cancel();
        Console.WriteLine("Stopping...");
    };

static INetcodeSession<GameInput> CreateNetcodeSession(Args args)
{
    var port = args.Port;

    // create rollback session builder
    var builder = RollbackNetcode
        .WithInputType<GameInput>()
        .WithPlayerCount(args.PlayerCount)
        .WithPort(port)
        .WithInputDelayFrames(2)
        .WithInitialRandomSeed(42)
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
    if (args.IsForSpectate(out var hostEndpoint))
        builder
            .WithFileLogWriter($"logs/log_game_spectator_{port}.log", append: false)
            .ForSpectator(hostEndpoint);

// not a spectator, creating a `remote` game session
    else if (args.IsForPlay(out var players))
        builder
            // Write logs in a file with player number
            .WithFileLogWriter($"logs/log_game_player_{port}.log", append: false)
            .WithPlayers(players)
            .ForRemote();

    else
        throw new InvalidOperationException("Invalid CLI arguments");

    return builder.Build();
}
