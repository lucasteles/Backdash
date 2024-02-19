using System.Numerics;
using Backdash;
using Backdash.Backends;
using Backdash.Core;
using Backdash.Network;

namespace ConsoleSession;

public class MyStateManager(
    Player localPlayer,
    Player remotePlayer,
    IRollbackSession<MyInput, MyState> session,
    ILogWriter logger
) : IRollbackHandler<MyState>
{
    const int GridSize = 5;

    // for debugging
    readonly bool waitEachFrame = false;

    bool synchronized;
    float syncPercent;
    readonly MyInput[] inputBuffer = new MyInput[2]; // used to read inputs from session


    MyState currentState = new()
    {
        Position1 = Vector2.Zero,
        Position2 = new Vector2(GridSize) - Vector2.One,
    };

    public void OnEvent(RollbackEventInfo evt)
    {
        logger.Write(LogLevel.Information, $"GAME => ROLLBACK EVENT {DateTime.UtcNow} = {evt}");

        switch (evt.Type)
        {
            case RollbackEvent.SynchronizedWithPeer:
                synchronized = true;
                syncPercent = 0;
                Draw();
                break;

            case RollbackEvent.SynchronizingWithPeer:
                syncPercent = evt.Synchronizing.CurrentStep /
                              (float) evt.Synchronizing.TotalSteps;
                break;
            case RollbackEvent.TimeSync:
                var framesAhead = evt.TimeSync.FramesAhead;
                Console.Write("** Catching up... **");
                Thread.Sleep(Frames.ToTimeSpan(framesAhead));
                break;
        }
    }

    public void SaveGameState(int frame, out MyState state) =>
        state = currentState;

    public void LoadGameState(in MyState gameState) =>
        currentState = gameState;

    public void AdvanceFrame()
    {
        session.SynchronizeInputs(inputBuffer);
        UpdateState(inputBuffer[0], inputBuffer[1]);
    }

    void UpdateState(MyInput player1, MyInput player2)
    {
        currentState = currentState with
        {
            Position1 = Move(currentState.Position1, player1),
            Position2 = Move(currentState.Position2, player2),
        };

        session.AdvanceFrame();
    }

    public void Update()
    {
        session.BeginFrame();
        session.GetInfo(remotePlayer, ref info);

        if (!synchronized)
            return;

        var myInput = ParseInput();

        var result = session.AddLocalInput(localPlayer, myInput);
        if (result is not ResultCode.Ok)
        {
            logger.Write(LogLevel.Warning, $"GAME => UNABLE TO ADD LOCAL INPUT: {result}");
            return;
        }

        if (session.SynchronizeInputs(inputBuffer) is not ResultCode.Ok)
        {
            logger.Write(LogLevel.Warning, $"GAME => UNABLE SYNC INPUTS: {result}");
            return;
        }

        UpdateState(inputBuffer[0], inputBuffer[1]);
    }

    static Vector2 Move(Vector2 pos, MyInput input)
    {
        var direction = Vector2.Zero;
        if (input.HasFlag(MyInput.Up))
            direction = -Vector2.UnitY;

        if (input.HasFlag(MyInput.Right))
            direction = Vector2.UnitX;

        if (input.HasFlag(MyInput.Down))
            direction = Vector2.UnitY;

        if (input.HasFlag(MyInput.Left))
            direction = -Vector2.UnitX;

        pos += direction;
        return Vector2.Clamp(pos, Vector2.Zero, new Vector2(GridSize - 1));
    }

    public MyInput ParseInput()
    {
        if (!waitEachFrame && !Console.KeyAvailable)
            return MyInput.None;

        return Console.ReadKey().Key switch
        {
            ConsoleKey.LeftArrow => MyInput.Left,
            ConsoleKey.RightArrow => MyInput.Right,
            ConsoleKey.UpArrow => MyInput.Up,
            ConsoleKey.DownArrow => MyInput.Down,
            _ => MyInput.None,
        };
    }

    readonly ConsoleColor defaultColor = ConsoleColor.Gray;
    readonly ConsoleColor[] playerColors = [ConsoleColor.Green, ConsoleColor.Red];

    public void Draw()
    {
        Console.Clear();
        Console.ForegroundColor = defaultColor;

        if (!synchronized)
        {
            DrawSync();
            return;
        }

        Console.ForegroundColor = playerColors[localPlayer.Number - 1];
        Console.WriteLine($"-- Player {localPlayer.Number} --\n");
        Console.ForegroundColor = defaultColor;

        for (var row = 0; row < GridSize; row++)
        {
            Console.Write(" ");
            for (var col = 0; col < GridSize; col++)
            {
                Console.Write(' ');
                if (DrawPlayer(currentState.Position1, col, row, playerColors[0]))
                    continue;

                if (DrawPlayer(currentState.Position2, col, row, playerColors[1]))
                    continue;

                Console.Write(".");
            }

            Console.WriteLine();
        }

        Console.WriteLine();
        DrawStats();
    }

    bool DrawPlayer(Vector2 pos, int col, int row, ConsoleColor color)
    {
        if ((int) pos.X == col && (int) pos.Y == row)
        {
            Console.ForegroundColor = color;
            Console.Write("0");
            Console.ForegroundColor = defaultColor;
            return true;
        }

        return false;
    }

    void DrawSync()
    {
        const int loadingSize = 20;
        if (syncPercent is 0)
        {
            Console.WriteLine("  --- Waiting Peer --- ");
            Console.Write("            ");
            Console.Write((DateTime.UtcNow.Second % 4) switch
            {
                0 => '/',
                1 => '-',
                2 => '\\',
                3 => '|',
                _ => '\0',
            });
            Console.WriteLine();
            return;
        }

        var loaded = loadingSize * syncPercent;
        Console.WriteLine("  --- Synchronizing... ---  ");
        Console.WriteLine();
        Console.Write(" ");
        for (var i = 0; i < loadingSize; i++)
        {
            Console.ForegroundColor = i <= loaded ? ConsoleColor.Green : ConsoleColor.White;
            Console.Write('\u2588');
        }

        Console.Write(Environment.NewLine);
    }

    RollbackSessionInfo info = new();

    void DrawStats()
    {
        if (!synchronized)
            return;

        Console.WriteLine(
            $"""
             Ping:      {info.Ping.TotalMilliseconds:f4}ms
             Pending:   {info.PendingInputCount}
             Pkg Sent:  {info.Pps:f2} pps
             Bandwidth: {info.BandwidthKbps:f2} Kbps
             Frame:     {info.CurrentFrame} / ack {info.LastAckedFrame} / send {info.LastSendFrame}
             Rollback:  {info.RollbackFrames} frames
             Advantage:  local({info.LocalFrameBehind}), remote({info.RemoteFrameBehind})
             """
        );
    }
}