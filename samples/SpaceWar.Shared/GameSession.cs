using Backdash;
using Backdash.Serialization;
using SpaceWar.Logic;

namespace SpaceWar;

public sealed class GameSession(
    GameState gameState,
    NonGameState nonGameState,
    Renderer renderer,
    INetcodeSession<PlayerInputs> session
) : INetcodeSessionHandler
{
    public void Update(TimeSpan deltaTime)
    {
        if (nonGameState.Sleeping)
        {
            nonGameState.SleepTime -= deltaTime;
            return;
        }

        UpdateStats();
        session.BeginFrame();

        var keyboard = Keyboard.GetState();
        HandleNoGameInput(keyboard);
        var localInput = Inputs.ReadInputs(keyboard);

        if (nonGameState.LocalPlayer is { } localPlayer
            && session.AddLocalInput(localPlayer, localInput) is not ResultCode.Ok)
            return;

        if (nonGameState.MirrorPlayer is { } mirrorPlayer &&
            session.AddLocalInput(mirrorPlayer, localInput) is not ResultCode.Ok)
            return;

        var syncInputResult = session.SynchronizeInputs();
        if (syncInputResult is not ResultCode.Ok)
        {
            // session.WriteLog(LogLevel.Warning, $"ERROR SYNC INPUTS: {syncInputResult}");
            return;
        }

        gameState.Update(session.CurrentSynchronizedInputs);
        session.AdvanceFrame();
    }

    void HandleNoGameInput(KeyboardState keyboard)
    {
        if (keyboard.IsKeyDown(Keys.D1)) DisconnectPlayer(0);
        if (keyboard.IsKeyDown(Keys.D2)) DisconnectPlayer(1);
        if (keyboard.IsKeyDown(Keys.D3)) DisconnectPlayer(2);
        if (keyboard.IsKeyDown(Keys.D4)) DisconnectPlayer(3);
    }

    void DisconnectPlayer(int index)
    {
        if (nonGameState.NumberOfPlayers <= index) return;
        var handle = nonGameState.Players[index].PlayerHandle;
        session.DisconnectPlayer(handle);
        nonGameState.StatusText.Clear();
        nonGameState.StatusText.Append("Disconnected player ");
        nonGameState.StatusText.Append(handle.Number);
    }

    public void Draw() => renderer.Draw(gameState, nonGameState);

    void Log(string message)
    {
        session.WriteLog(message);
        Console.WriteLine(message);
    }

    public void SaveState(in Frame frame, ref readonly BinaryBufferWriter writer)
    {
        // UNCOMMENT TO FORCE DESYNC ON FRAME 200
#pragma warning disable S125
        // if (frame.Number is 200)
        //     gameState.FrameNumber = Random.Shared.Next(1000, 2000);
#pragma warning restore S125

        gameState.SaveState(in writer);
    }

    public void LoadState(in Frame frame, ref readonly BinaryBufferReader reader)
    {
        Log($"=> LOADING STATE {frame}...");
        gameState.LoadState(in reader);
    }

    public void AdvanceFrame()
    {
        session.SynchronizeInputs();
        gameState.Update(session.CurrentSynchronizedInputs);
        session.AdvanceFrame();
    }

    public void OnSessionStart()
    {
        Log("=> GAME STARTED");
        nonGameState.SetConnectState(PlayerConnectState.Running);
        nonGameState.StatusText.Clear();
    }

    public void OnSessionClose()
    {
        Log("=> GAME CLOSED");
        nonGameState.SetConnectState(PlayerConnectState.Disconnected);
    }

    public void TimeSync(FrameSpan framesAhead)
    {
        Log($"=> TIME SYNC REQUESTED {framesAhead.FrameCount}");
        nonGameState.SleepTime = framesAhead.Duration();
    }

    void UpdateStats()
    {
        nonGameState.RollbackFrames = session.RollbackFrames;
        var saved = session.GetCurrentSavedFrame();
        nonGameState.StateChecksum = saved.Checksum;
        nonGameState.StateSize = saved.Size;
    }

    public void OnPeerEvent(NetcodePlayer player, PeerEventInfo evt)
    {
        Log($"=> PEER EVENT: {evt} from {player}");
        if (player.IsSpectator()) return;

        switch (evt.Type)
        {
            case PeerEvent.Connected:
                nonGameState.SetConnectState(player, PlayerConnectState.Synchronizing);
                break;
            case PeerEvent.Synchronizing:
                var progress = 100 * evt.Synchronizing.CurrentStep /
                               (float)evt.Synchronizing.TotalSteps;
                nonGameState.UpdateConnectProgress(player, (int)progress);
                break;
            case PeerEvent.SynchronizationFailure:
                nonGameState.SetConnectState(player, PlayerConnectState.Disconnected);
                break;
            case PeerEvent.Synchronized:
                nonGameState.UpdateConnectProgress(player, 100);
                break;
            case PeerEvent.ConnectionInterrupted:
                nonGameState.SetDisconnectTimeout(
                    player, DateTime.UtcNow,
                    evt.ConnectionInterrupted.DisconnectTimeout
                );
                break;
            case PeerEvent.ConnectionResumed:
                nonGameState.SetConnectState(player, PlayerConnectState.Running);
                break;
            case PeerEvent.Disconnected:
                nonGameState.SetConnectState(player, PlayerConnectState.Disconnected);
                break;
        }
    }

    // used by SyncTest, the return value is used on the state desync handler call
    object INetcodeSessionHandler.CreateState(in Frame frame, ref readonly BinaryBufferReader reader)
    {
        GameState state = new();
        state.LoadState(in reader);
        return state;
    }
}
