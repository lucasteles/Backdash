using System.Diagnostics;
using Backdash;
using Backdash.Backends;
using Backdash.Data;


namespace ConsoleSession;

public sealed class Game : IRollbackHandler<GameState>
{
    // Rollback NetCode Session
    readonly IRollbackSession<GameInput> session;

    // State that does not affect the game
    readonly NonGameState nonGameState;

    // Draw the game state on console
    readonly View view;

    // Actual game state
    GameState currentState = GameLogic.InitialState();

    // buffer for input reading from session
    readonly GameInput[] inputBuffer = new GameInput[2];

    public Game(IRollbackSession<GameInput> session)
    {
        view = new View();
        this.session = session;

        var players = session.GetPlayers();
        nonGameState = new()
        {
            LocalPlayer = players.Single(x => x.IsLocal()),
            RemotePlayer = players.Single(x => x.IsRemote()),
            SessionInfo = session,
        };
    }

    // Game Loop
    public void Update()
    {
        session.BeginFrame();

        if (nonGameState.IsRunning)
        {
            var localInput = GameLogic.ReadKeyboardInput();

            var result = session.AddLocalInput(nonGameState.LocalPlayer, localInput);

            if (result is not ResultCode.Ok)
            {
                Log($"UNABLE TO ADD INPUT: {result}");
                nonGameState.LastError =
                    @$"{result} {Stopwatch.GetElapsedTime(nonGameState.StartedAt):mm\:ss\.fff}";
                return;
            }

            if (session.SynchronizeInputs(inputBuffer) is not ResultCode.Ok)
            {
                Log($"UNABLE SYNC INPUTS: {result}");
                nonGameState.LastError =
                    @$"{result} {Stopwatch.GetElapsedTime(nonGameState.StartedAt):mm\:ss\.fff}";
                return;
            }

            GameLogic.AdvanceState(ref currentState, inputBuffer[0], inputBuffer[1]);

            session.AdvanceFrame();
        }

        session.GetNetworkStatus(nonGameState.RemotePlayer, ref nonGameState.PeerNetworkStatus);

        view.Draw(in currentState, nonGameState);
    }

    static void Log(string message) =>
        Trace.WriteLine($"{DateTime.UtcNow:hh:mm:ss.zzz} GAME => {message}");

    // Session Callbacks
    public void Start()
    {
        Log("START");
        nonGameState.StartedAt = Stopwatch.GetTimestamp();
        nonGameState.IsRunning = true;
    }

    public void TimeSync(FrameSpan framesAhead)
    {
        Console.SetCursorPosition(1, 0);
        Console.WriteLine("Syncing...");
        Thread.Sleep(framesAhead.Duration);
    }

    public void OnPeerEvent(PlayerHandle player, PeerEventInfo evt)
    {
        Log($"PEER EVENT: {evt}");

        switch (evt.Type)
        {
            case PeerEvent.Synchronizing:
                nonGameState.SyncPercent =
                    evt.Synchronizing.CurrentStep / (float) evt.Synchronizing.TotalSteps;
                break;

            case PeerEvent.Synchronized:
                nonGameState.SyncPercent = 0;
                view.Draw(in currentState, nonGameState);
                break;
        }
    }

    public void SaveState(int frame, ref GameState state)
    {
        state.Position1 = currentState.Position1;
        state.Position2 = currentState.Position2;
    }

    public void LoadState(in GameState gameState)
    {
        currentState.Position1 = gameState.Position1;
        currentState.Position2 = gameState.Position2;
    }

    public void AdvanceFrame()
    {
        session.SynchronizeInputs(inputBuffer);
        GameLogic.AdvanceState(ref currentState, inputBuffer[0], inputBuffer[1]);
        session.AdvanceFrame();
    }
}