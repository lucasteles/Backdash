using System.Text;
using Backdash;
using Backdash.Data;

namespace SpaceWar.Logic;

public enum PlayerConnectState
{
    Connecting,
    Synchronizing,
    Running,
    Disconnected,
    Disconnecting,
}

public class PlayerConnectionInfo
{
    public string? Name;
    public PlayerHandle Handle;
    public PlayerConnectState State;
    public int ConnectProgress;
    public DateTime DisconnectStart;
    public TimeSpan DisconnectTimeout;
    public readonly StringBuilder StatusText = new();
    public RollbackNetworkStatus PeerNetworkStatus = new();
}

public class NonGameState(int numberOfPlayers, Rectangle window)
{
    public readonly PlayerConnectionInfo[] Players = new PlayerConnectionInfo[numberOfPlayers];
    public readonly Background Background = new(window);
    public readonly StringBuilder StatusText = new();
    public PlayerHandle? LocalPlayerHandle;
    public TimeSpan SleepTime;
    public bool Sleeping => SleepTime > TimeSpan.Zero;
    public int NumberOfPlayers => numberOfPlayers;
    public FrameSpan RollbackFrames;

    public bool TryGetPlayer(PlayerHandle handle, out PlayerConnectionInfo state)
    {
        for (var i = 0; i < Players.Length; i++)
        {
            if (Players[i].Handle != handle) continue;
            state = Players[i];
            return true;
        }

        state = null!;
        return false;
    }

    public void SetDisconnectTimeout(PlayerHandle handle, DateTime when, TimeSpan timeout)
    {
        if (!TryGetPlayer(handle, out var player)) return;
        player.DisconnectStart = when;
        player.DisconnectTimeout = timeout;
        player.State = PlayerConnectState.Disconnecting;
    }

    public void SetConnectState(in PlayerHandle handle, PlayerConnectState state)
    {
        if (!TryGetPlayer(handle, out var player)) return;
        player.ConnectProgress = 0;
        player.State = state;
    }

    public void SetConnectState(PlayerConnectState state)
    {
        foreach (var player in Players)
            player.State = state;
    }

    public void UpdateConnectProgress(PlayerHandle handle, int progress)
    {
        if (!TryGetPlayer(handle, out var player)) return;
        player.ConnectProgress = progress;
    }
}
