using System.Numerics;
using Backdash;
namespace ConsoleGame;

[Flags]
public enum GameInput
{
    None = 0,
    Up = 1 << 0,
    Down = 1 << 1,
    Left = 1 << 2,
    Right = 1 << 3,
}

[Serializable]
public record struct GameState
{
    public Vector2 Position1;
    public Vector2 Position2;
    public int Score1;
    public int Score2;
    public Vector2 Target;
}

[Serializable]
public class NonGameState
{
    public required PlayerHandle? LocalPlayer;
    public required PlayerHandle RemotePlayer;
    public required IRollbackSessionInfo SessionInfo;
    public bool IsRunning;
    public float SyncProgress;
    public string LastError = "";
    public PlayerStatus RemotePlayerStatus;
    public PeerNetworkStats PeerNetworkStats = new();
    public DateTime LostConnectionTime;
    public TimeSpan DisconnectTimeout;
}

public enum PlayerStatus
{
    Connecting = 0,
    Synchronizing,
    Running,
    Waiting,
    Disconnected,
}
