using System.Numerics;
using Backdash;
using Backdash.Network;

namespace ConsoleSession;

public class View
{
    public const int GridSize = 5;

    readonly ConsoleColor defaultColor = ConsoleColor.Gray;
    readonly ConsoleColor[] playerColors = [ConsoleColor.Green, ConsoleColor.Red];

    public View() => Console.CursorVisible = false;

    public void Draw(in GameState currentState, NonGameState nonGameState)
    {
        Console.Clear();
        Console.ForegroundColor = defaultColor;

        if (!nonGameState.IsRunning)
        {
            DrawSync(nonGameState);
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

        if (nonGameState.IsRunning)
            DrawStats(nonGameState);
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

    void DrawSync(NonGameState nonGameState)
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

    void DrawStats(NonGameState nonGameState)
    {
        var peer = nonGameState.PeerNetworkStatus;
        var info = nonGameState.SessionInfo;

        Console.WriteLine(
            $"""
             FPS:              {info.FramesPerSecond}
             Ping:             {peer.Ping.TotalMilliseconds:f4} ms
             Pending Inputs:   {peer.PendingInputCount}
             Rollback:         {info.RollbackFrames}
             Frame:            {info.CurrentFrame.Number} ack({peer.LastAckedFrame.Number}) send({peer.Send.LastFrame.Number})
             Advantage:        local({peer.LocalFramesBehind}) remote({peer.RemoteFramesBehind})
             Pkg Count out/in: {peer.Send.PackagesPerSecond:f2} pps / {peer.Received.PackagesPerSecond:f2} pps
             Bandwidth out/in: {peer.Send.Bandwidth.KibiBytes:f2} Kbps / {peer.Received.Bandwidth.KibiBytes:f2} Kbps
             Last Error: {nonGameState.LastError}
             """
        );
    }
}