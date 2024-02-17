using System.Diagnostics;
using Backdash;
using Backdash.Backends;
using Backdash.Core;

namespace ConsoleSession;

public sealed class Game : IDisposable
{
    const int NumberOfPlayers = 2;
    readonly MyStateManager gameState;
    readonly IRollbackSession<MyInput, MyState> session;
    readonly ILogWriter logger = new DebuggerLogger();

    public Game(IReadOnlyList<string> args)
    {
        if (args is not [{ } portArg, { } peer1Arg, { } peer2Arg]
            || !int.TryParse(portArg, out var port)
            || !Util.TryParsePlayer(1, peer1Arg, out var player1)
            || !Util.TryParsePlayer(2, peer2Arg, out var player2)
           )
            throw new InvalidOperationException("Invalid arguments...");

        Player[] players = [player1, player2];
        var localPlayer = players.Single(x => x.Type is PlayerType.Local);
        var remotePlayer = players.Single(x => x.Type is PlayerType.Remote);

        session = Rollback.CreateSession<MyInput, MyState>(
            new(port, NumberOfPlayers)
            {
                LogLevel = LogLevel.Information,
                FrameDelay = 2,
                Protocol = new()
                {
                    NumberOfSyncPackets = 100,
                },
            },
            logWriter: logger
            // , stateSerializer: new MyStateSerializer()
        );

        gameState = new(localPlayer, remotePlayer, session, logger);
        session.SetHandler(gameState);
        Trace.Assert(session.AddPlayer(player1) is ResultCode.Ok);
        Trace.Assert(session.AddPlayer(player2) is ResultCode.Ok);
        session.Start(); // start background work
    }

    public void Dispose() => session.Dispose();

    public void Update(TimeSpan deltaTime)
    {
        gameState.Update();
        gameState.Draw();
    }
}