using System.Numerics;
using Backdash;
using Backdash.Network;

namespace ConsoleSession;

[Flags]
public enum GameInput
{
    None = 0,
    Up = 1 << 0,
    Down = 1 << 1,
    Left = 1 << 2,
    Right = 1 << 3,
}

public record struct GameState()
{
    public Vector2 Position1 = Vector2.Zero;
    public Vector2 Position2 = Vector2.Zero;
}

public class NonGameState
{
    public bool IsRunning;
    public long StartedAt;
    public float SyncPercent;
    public string LastError = "";
    public required PlayerHandle LocalPlayer;
    public required PlayerHandle RemotePlayer;
    public RollbackSessionInfo Stats = new();
}