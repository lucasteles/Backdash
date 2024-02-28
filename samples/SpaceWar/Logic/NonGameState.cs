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
    public PlayerHandle Handle;
    public PlayerConnectState State;
    public int ConnectProgress;
    public TimeSpan DisconnectTimeout;
    public long DisconnectStart;
}

public readonly record struct ChecksumInfo(Frame FrameNumber, int Checksum);

public class NonGameState(int numberOfPlayers, PlayerHandle localPlayer)
{
    public readonly PlayerHandle LocalPlayerHandle = localPlayer;
    public readonly PlayerConnectionInfo[] Players = new PlayerConnectionInfo[numberOfPlayers];
    public int NumberOfPlayers => numberOfPlayers;
    public ChecksumInfo Now;
    public ChecksumInfo Periodic;

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

    public void SetConnectState(in PlayerHandle handle, PlayerConnectState state)
    {
        if (!TryGetPlayer(handle, out var player)) return;
        player.ConnectProgress = 0;
        player.State = state;
    }

    public void SetDisconnectTimeout(PlayerHandle handle, long when, TimeSpan timeout)
    {
        if (!TryGetPlayer(handle, out var player)) return;
        player.DisconnectStart = when;
        player.DisconnectTimeout = timeout;
        player.State = PlayerConnectState.Disconnecting;
    }

    void SetConnectState(PlayerConnectState state)
    {
        foreach (var player in Players)
            player.State = state;
    }

    void UpdateConnectProgress(PlayerHandle handle, int progress)
    {
        if (!TryGetPlayer(handle, out var player)) return;
        player.ConnectProgress = progress;
    }
}