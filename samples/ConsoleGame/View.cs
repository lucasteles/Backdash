using System.Numerics;

namespace ConsoleGame;

using static Console;

public class View
{
    const ConsoleColor DefaultColor = ConsoleColor.Gray;
    const ConsoleColor TargetColor = ConsoleColor.Yellow;
    readonly ConsoleColor[] playerColors = [ConsoleColor.Green, ConsoleColor.Red];
    public View() => CursorVisible = false;

    public void Draw(in GameState currentState, NonGameState nonGameState)
    {
        Clear();
        DrawHeader(nonGameState);
        DrawConnection(nonGameState);
        DrawField(in currentState, nonGameState);
        DrawScore(in currentState);
        if (nonGameState.RemotePlayerStatus is PlayerStatus.Running)
            DrawStats(currentState, nonGameState);
    }

    void DrawHeader(NonGameState nonGameState)
    {
        if (nonGameState.LocalPlayer is { } localPlayer)
        {
            Title = $"Player {localPlayer.Number}";
            ForegroundColor = playerColors[localPlayer.Index];
            WriteLine($"-- Player {localPlayer.Number} --\n");
        }
        else
        {
            Title = "Spectator";
            ForegroundColor = ConsoleColor.Magenta;
            WriteLine("-- Spectator --\n");
        }

        ForegroundColor = DefaultColor;
    }

    void DrawScore(in GameState state)
    {
        Write("Score: ");
        ForegroundColor = playerColors[0];
        Write($"{state.Score1,-2:00}");
        ForegroundColor = DefaultColor;
        Write(" X ");
        ForegroundColor = playerColors[1];
        Write($"{state.Score2,-2:00}");
        ForegroundColor = DefaultColor;
        WriteLine();
    }

    void DrawField(in GameState currentState, NonGameState nonGameState)
    {
        var status1 = nonGameState.RemotePlayer.Index is 0
            ? nonGameState.RemotePlayerStatus
            : PlayerStatus.Running;
        var status2 = nonGameState.RemotePlayer.Index is 1
            ? nonGameState.RemotePlayerStatus
            : PlayerStatus.Running;
        for (var row = 0; row < GameLogic.GridSize; row++)
        {
            Write(" ");
            for (var col = 0; col < GameLogic.GridSize; col++)
            {
                Write(' ');
                if (DrawPlayer(currentState.Position1, col, row, playerColors[0], status1))
                    continue;
                if (DrawPlayer(currentState.Position2, col, row, playerColors[1], status2))
                    continue;
                if ((int)currentState.Target.X == col && (int)currentState.Target.Y == row)
                {
                    ForegroundColor = TargetColor;
                    Write('*');
                    ForegroundColor = DefaultColor;
                    continue;
                }

                Write(".");
            }

            WriteLine();
        }

        WriteLine();
    }

    static bool DrawPlayer(Vector2 pos, int col, int row, ConsoleColor color, PlayerStatus status)
    {
        if ((int)pos.X == col && (int)pos.Y == row)
        {
            ForegroundColor = color;
            Write(status switch
            {
                PlayerStatus.Running => "0",
                PlayerStatus.Disconnected => "X",
                _ => "?",
            });
            ForegroundColor = DefaultColor;
            return true;
        }

        return false;
    }

    static void DrawConnection(NonGameState nonGameState)
    {
        Write(" ");
        switch (nonGameState.RemotePlayerStatus)
        {
            case PlayerStatus.Connecting:
                Write("Connecting");
                for (var i = 0; i < DateTime.UtcNow.Second % 4; i++)
                    Write('.');
                WriteLine();
                break;
            case PlayerStatus.Synchronizing:
                Write("Synchronizing:");
                DrawProgressBar(nonGameState.SyncProgress);
                WriteLine();
                break;
            case PlayerStatus.Running:
                ForegroundColor = ConsoleColor.Cyan;
                WriteLine("Connected.");
                break;
            case PlayerStatus.Waiting:
                ForegroundColor = ConsoleColor.DarkYellow;
                Write("Waiting:");
                var progress = (DateTime.UtcNow - nonGameState.LostConnectionTime)
                    .TotalMilliseconds / nonGameState.DisconnectTimeout.TotalMilliseconds;
                DrawProgressBar(progress);
                WriteLine();
                break;
            case PlayerStatus.Disconnected:
                ForegroundColor = ConsoleColor.Red;
                WriteLine("Disconnected.");
                break;
        }

        ForegroundColor = DefaultColor;
        WriteLine();
    }

    static void DrawProgressBar(double percent)
    {
        const int loadingSize = 10;
        var loaded = loadingSize * percent;
        var lastColor = ForegroundColor;
        Write(' ');
        for (var i = 0; i < loadingSize; i++)
        {
            ForegroundColor = i <= loaded ? ConsoleColor.DarkGreen : ConsoleColor.White;
            Write('\u2588');
        }

        ForegroundColor = lastColor;
    }

    static void DrawStats(GameState currentState, NonGameState nonGameState)
    {
        if (nonGameState.RemotePlayer is not { NetworkStats: { } peer })
            return;

        var info = nonGameState.SessionInfo;
        WriteLine(
            $"""
             Ping:             {peer.Ping.TotalMilliseconds:f4} ms
             Rollback:         {info.RollbackFrames}
             Checksum:         {nonGameState.Checksum:x8}
             Rng Seed:         {currentState.RandomSeed:x8}
             """
        );
#if DEBUG
        WriteLine(
            $"""
             Pending Inputs:   {peer.PendingInputCount}
             Frame:            {info.CurrentFrame.Number} ack({peer.LastAckedFrame.Number}) send({peer.Send.LastFrame.Number})
             Advantage:        local({peer.LocalFramesBehind}) remote({peer.RemoteFramesBehind})
             Pkg Count out/in: {peer.Send.PackagesPerSecond:f2} pps / {peer.Received.PackagesPerSecond:f2} pps
             Bandwidth out/in: {peer.Send.Bandwidth.KibiBytes:f2} Kbps / {peer.Received.Bandwidth.KibiBytes:f2} Kbps
             """
        );
        if (!string.IsNullOrWhiteSpace(nonGameState.LastError))
            WriteLine($"Last Error:       {nonGameState.LastError}");
#endif
    }
}
