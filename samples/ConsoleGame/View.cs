using System.Diagnostics;
using System.Numerics;

namespace ConsoleGame;

public class View
{
    readonly ConsoleColor defaultColor = ConsoleColor.Gray;
    readonly ConsoleColor targetColor = ConsoleColor.Yellow;
    readonly ConsoleColor[] playerColors = [ConsoleColor.Green, ConsoleColor.Red];

    public View() => Console.CursorVisible = false;

    public void Draw(in GameState currentState, NonGameState nonGameState)
    {
        Console.Clear();

        DrawHeader(nonGameState);
        DrawConnection(nonGameState);
        DrawField(in currentState, nonGameState);
        DrawScore(in currentState);
        if (nonGameState.RemotePlayerStatus is PlayerStatus.Running)
            DrawStats(nonGameState);
    }

    void DrawHeader(NonGameState nonGameState)
    {
        if (nonGameState.LocalPlayer is { } localPlayer)
        {
            Console.Title = $"Player {localPlayer.Number}";
            Console.ForegroundColor = playerColors[localPlayer.Number - 1];
            Console.WriteLine($"-- Player {localPlayer.Number} --\n");
        }
        else
        {
            Console.Title = "Spectator";
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine("-- Spectator --\n");
        }

        Console.ForegroundColor = defaultColor;
    }

    void DrawScore(in GameState state)
    {
        Console.Write("Score: ");
        Console.ForegroundColor = playerColors[0];
        Console.Write($"{state.Score1,-2:00}");
        Console.ForegroundColor = defaultColor;
        Console.Write(" X ");
        Console.ForegroundColor = playerColors[1];
        Console.Write($"{state.Score2,-2:00}");
        Console.ForegroundColor = defaultColor;
        Console.WriteLine();
    }

    void DrawField(in GameState currentState, NonGameState nonGameState)
    {
        var status1 = nonGameState.RemotePlayer.Number is 1
            ? nonGameState.RemotePlayerStatus
            : PlayerStatus.Running;

        var status2 = nonGameState.RemotePlayer.Number is 2
            ? nonGameState.RemotePlayerStatus
            : PlayerStatus.Running;

        for (var row = 0; row < GameLogic.GridSize; row++)
        {
            Console.Write(" ");
            for (var col = 0; col < GameLogic.GridSize; col++)
            {
                Console.Write(' ');
                if (DrawPlayer(currentState.Position1, col, row, playerColors[0], status1))
                    continue;

                if (DrawPlayer(currentState.Position2, col, row, playerColors[1], status2))
                    continue;

                if ((int)currentState.Target.X == col && (int)currentState.Target.Y == row)
                {
                    Console.ForegroundColor = targetColor;
                    Console.Write('*');
                    Console.ForegroundColor = defaultColor;
                    continue;
                }

                Console.Write(".");
            }

            Console.WriteLine();
        }

        Console.WriteLine();
    }

    bool DrawPlayer(Vector2 pos, int col, int row, ConsoleColor color, PlayerStatus status)
    {
        if ((int)pos.X == col && (int)pos.Y == row)
        {
            Console.ForegroundColor = color;
            Console.Write(status switch
            {
                PlayerStatus.Running => "0",
                PlayerStatus.Disconnected => "X",
                _ => "?",
            });
            Console.ForegroundColor = defaultColor;
            return true;
        }

        return false;
    }

    void DrawConnection(NonGameState nonGameState)
    {
        Console.Write(" ");
        switch (nonGameState.RemotePlayerStatus)
        {
            case PlayerStatus.Connecting:
                Console.Write("Connecting");
                for (var i = 0; i < DateTime.UtcNow.Second % 4; i++)
                    Console.Write('.');
                Console.WriteLine();
                break;
            case PlayerStatus.Synchronizing:
                Console.Write("Synchronizing:");
                DrawProgressBar(nonGameState.SyncProgress);
                Console.WriteLine();
                break;
            case PlayerStatus.Running:
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Connected.");
                break;
            case PlayerStatus.Waiting:
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write("Waiting:");
                var progress = (DateTime.UtcNow - nonGameState.LostConnectionTime)
                    .TotalMilliseconds / nonGameState.DisconnectTimeout.TotalMilliseconds;
                DrawProgressBar(progress);
                Console.WriteLine();
                break;
            case PlayerStatus.Disconnected:
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Disconnected.");
                break;
        }

        Console.ForegroundColor = defaultColor;
        Console.WriteLine();
    }

    static void DrawProgressBar(double percent)
    {
        const int loadingSize = 10;
        var loaded = loadingSize * percent;
        var lastColor = Console.ForegroundColor;
        Console.Write(" ");

        for (var i = 0; i < loadingSize; i++)
        {
            Console.ForegroundColor = i <= loaded ? ConsoleColor.DarkGreen : ConsoleColor.White;
            Console.Write('\u2588');
        }

        Console.ForegroundColor = lastColor;
    }

    void DrawStats(NonGameState nonGameState)
    {
        var peer = nonGameState.PeerNetworkStatus;
        var info = nonGameState.SessionInfo;


        Console.WriteLine(
            $"""
             Ping:             {peer.Ping.TotalMilliseconds:f4} ms
             Rollback:         {info.RollbackFrames}
             """
        );

#if DEBUG
        Console.WriteLine(
            $"""
             Pending Inputs:   {peer.PendingInputCount}
             FPS:              {info.FramesPerSecond}
             Frame:            {info.CurrentFrame.Number} ack({peer.LastAckedFrame.Number}) send({peer.Send.LastFrame.Number})
             Advantage:        local({peer.LocalFramesBehind}) remote({peer.RemoteFramesBehind})
             Pkg Count out/in: {peer.Send.PackagesPerSecond:f2} pps / {peer.Received.PackagesPerSecond:f2} pps
             Bandwidth out/in: {peer.Send.Bandwidth.KibiBytes:f2} Kbps / {peer.Received.Bandwidth.KibiBytes:f2} Kbps
             """
        );

        if (!string.IsNullOrWhiteSpace(nonGameState.LastError))
            Console.WriteLine($"Last Error:       {nonGameState.LastError}");
#endif
    }
}