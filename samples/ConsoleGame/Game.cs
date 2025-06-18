using System.Diagnostics;
using Backdash;
using Backdash.Serialization;
using Backdash.Serialization.Numerics;

namespace ConsoleGame;

public sealed class Game : INetcodeSessionHandler
{
    // Rollback Net-code session callbacks
    readonly INetcodeSession<GameInput> session;

    readonly CancellationTokenSource cancellation;

    // State that does not affect the game
    readonly NonGameState nonGameState;

    // Draw the game state on console
    readonly View view;

    // Actual game state
    GameState currentState;

    public Game(INetcodeSession<GameInput> session, CancellationTokenSource cancellation)
    {
        view = new();
        this.session = session;
        this.cancellation = cancellation;

        currentState = GameLogic.InitialState();

        if (session.IsRemote())
        {
            if (!session.TryGetLocalPlayer(out var localPlayer))
                throw new InvalidOperationException("Local player not found");

            if (!session.TryGetRemotePlayer(out var remote))
                throw new InvalidOperationException("Remote player not found");

            nonGameState = new()
            {
                LocalPlayer = localPlayer,
                RemotePlayer = remote,
                SessionInfo = session,
            };
        }
        else if (session.IsSpectator())
        {
            nonGameState = new()
            {
                LocalPlayer = null,
                RemotePlayer = new(),
                SessionInfo = session,
            };
        }
        else
            throw new InvalidOperationException($"not supported session mode: {session.Mode}");
    }

    // Game Loop
    public void Update()
    {
        session.BeginFrame();

        if (nonGameState.IsRunning)
            UpdateState();

        view.Draw(in currentState, nonGameState);
    }

    void UpdateState()
    {
        if (nonGameState.LocalPlayer is { } localPlayer)
        {
            var localInput = GameLogic.ReadKeyboardInput(out var disconnectRequest);
            if (disconnectRequest)
                cancellation.Cancel();

            var result = session.AddLocalInput(localPlayer, localInput);
            if (result is not ResultCode.Ok)
            {
                Log($"UNABLE TO ADD INPUT: {result}");
                nonGameState.LastError = @$"{result} {DateTime.Now:mm\:ss\.fff}";
                return;
            }
        }

        nonGameState.Checksum = session.CurrentChecksum;
        session.SetRandomSeed(currentState.RandomSeed);
        var syncResult = session.SynchronizeInputs();
        if (syncResult is not ResultCode.Ok)
        {
            Log($"UNABLE SYNC INPUTS: {syncResult}");
            nonGameState.LastError = @$"{syncResult} {DateTime.Now:mm\:ss\.fff}";
            return;
        }

        GameLogic.Update(
            session.Random,
            ref currentState,
            session.GetInput(0),
            session.GetInput(1)
        );

        session.AdvanceFrame();
    }

    static void Log(string message) =>
        Trace.TraceInformation($"{DateTime.UtcNow:hh:mm:ss.zzz} GAME => {message}");

    // Session Callbacks
    public void OnSessionStart()
    {
        Log("GAME STARTED");
        nonGameState.IsRunning = true;
        nonGameState.RemotePlayerStatus = PlayerStatus.Running;
    }

    public void OnSessionClose()
    {
        Log("GAME CLOSED");
        nonGameState.IsRunning = false;
        nonGameState.RemotePlayerStatus = PlayerStatus.Disconnected;
    }

    public void SaveState(in Frame frame, ref readonly BinaryBufferWriter writer)
    {
        writer.Write(currentState.Position1);
        writer.Write(currentState.Position2);
        writer.Write(currentState.Score1);
        writer.Write(currentState.Score2);
        writer.Write(currentState.RandomSeed);
        writer.Write(currentState.Target);
    }

    public void LoadState(in Frame frame, ref readonly BinaryBufferReader reader)
    {
        currentState.Position1 = reader.ReadVector2();
        currentState.Position2 = reader.ReadVector2();
        currentState.Score1 = reader.ReadInt32();
        currentState.Score2 = reader.ReadInt32();
        currentState.RandomSeed = reader.ReadUInt32();
        currentState.Target = reader.ReadVector2();
    }

    public void TimeSync(FrameSpan framesAhead)
    {
        Console.SetCursorPosition(1, 0);
        Console.WriteLine("> Syncing...");
        Thread.Sleep(framesAhead.Duration());
    }

    public void OnPeerEvent(NetcodePlayer player, PeerEventInfo evt)
    {
        Log($"PEER EVENT: {evt} from {player}");
        if (player.IsSpectator())
            return;
        switch (evt.Type)
        {
            case PeerEvent.Connected:
                nonGameState.RemotePlayerStatus = PlayerStatus.Synchronizing;
                break;
            case PeerEvent.Synchronizing:
                nonGameState.SyncProgress =
                    evt.Synchronizing.CurrentStep / (float)evt.Synchronizing.TotalSteps;
                break;
            case PeerEvent.Synchronized:
                nonGameState.SyncProgress = 1;
                break;
            case PeerEvent.ConnectionInterrupted:
                nonGameState.RemotePlayerStatus = PlayerStatus.Waiting;
                nonGameState.LostConnectionTime = DateTime.UtcNow;
                nonGameState.DisconnectTimeout = evt.ConnectionInterrupted.DisconnectTimeout;
                break;
            case PeerEvent.ConnectionResumed:
                nonGameState.RemotePlayerStatus = PlayerStatus.Running;
                break;
            case PeerEvent.Disconnected:
                nonGameState.RemotePlayerStatus = PlayerStatus.Disconnected;
                nonGameState.IsRunning = false;
                break;
        }
    }

    public void AdvanceFrame()
    {
        nonGameState.Checksum = session.CurrentChecksum;
        session.SetRandomSeed(currentState.RandomSeed);
        session.SynchronizeInputs();
        var (input1, input2) = (session.GetInput(0), session.GetInput(1));
        GameLogic.Update(session.Random, ref currentState, input1, input2);
        session.AdvanceFrame();
    }

    public async Task Run(CancellationToken cancellationToken)
    {
        var frameDuration = FrameTime.RateStep(60);

        try
        {
            using PeriodicTimer timer = new(frameDuration);
            do Update();
            while (await timer.WaitForNextTickAsync(cancellationToken));
        }
        catch (OperationCanceledException)
        {
            // skip
        }
    }
}
