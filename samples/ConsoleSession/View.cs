using System.Numerics;

namespace ConsoleSession;

public class View(NonGameState nonGameState)
{
    public const int GridSize = 5;

    readonly ConsoleColor defaultColor = ConsoleColor.Gray;
    readonly ConsoleColor[] playerColors = [ConsoleColor.Green, ConsoleColor.Red];

    public void Draw(in GameState currentState)
    {
        Console.Clear();
        Console.ForegroundColor = defaultColor;

        if (!nonGameState.IsRunning)
        {
            DrawSync();
            return;
        }

        Console.ForegroundColor = playerColors[nonGameState.LocalPlayer.Number - 1];
        Console.WriteLine($"-- Player {nonGameState.LocalPlayer.Number} --\n");
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
        if ((int)pos.X == col && (int)pos.Y == row)
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
        if (nonGameState.SyncPercent is 0)
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

        var loaded = loadingSize * nonGameState.SyncPercent;
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

    void DrawStats()
    {
        if (!nonGameState.IsRunning)
            return;

        var info = nonGameState.Stats;

        Console.WriteLine(
            $"""
             FPS:       {info.FramesPerSecond}
             Ping:      {info.Ping.TotalMilliseconds:f4}ms
             Pending:   {info.PendingInputCount}
             Pkg Sent:  {info.Pps:f2} pps
             Bandwidth: {info.BandwidthKbps:f2} Kbps
             Frame:     {info.CurrentFrame} / ack {info.LastAckedFrame} / send {info.LastSendFrame}
             Rollback:  {info.RollbackFrames} frames
             Advantage:  local({info.LocalFrameBehind}), remote({info.RemoteFrameBehind})
             Last Error: {nonGameState.LastError}
             """
        );
    }
}