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

public class PlayerInfo(NetcodePlayer player)
{
    public string? Name;
    public NetcodePlayer PlayerHandle = player;
    public PlayerConnectState State;
    public int ConnectProgress;
    public DateTime DisconnectStart;
    public TimeSpan DisconnectTimeout;
    public readonly StringBuilder StatusText = new();
}

public class NonGameState(int numberOfPlayers)
{
    public readonly PlayerInfo[] Players = new PlayerInfo[numberOfPlayers];
    public readonly Background Background = new();
    public readonly StringBuilder StatusText = new();
    public NetcodePlayer? LocalPlayer;
    public NetcodePlayer? MirrorPlayer;
    public TimeSpan SleepTime;
    public bool Sleeping => SleepTime > TimeSpan.Zero;
    public int NumberOfPlayers => numberOfPlayers;
    public FrameSpan RollbackFrames;
    public uint StateChecksum;
    public ByteSize StateSize;

    public bool TryGetPlayer(NetcodePlayer handle, out PlayerInfo state)
    {
        for (var i = 0; i < Players.Length; i++)
        {
            if (Players[i].PlayerHandle != handle) continue;
            state = Players[i];
            return true;
        }

        state = null!;
        return false;
    }

    public void SetDisconnectTimeout(NetcodePlayer handle, DateTime when, TimeSpan timeout)
    {
        if (!TryGetPlayer(handle, out var player)) return;
        player.DisconnectStart = when;
        player.DisconnectTimeout = timeout;
        player.State = PlayerConnectState.Disconnecting;
    }

    public void SetConnectState(NetcodePlayer handle, PlayerConnectState state)
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

    public void UpdateConnectProgress(NetcodePlayer handle, int progress)
    {
        if (!TryGetPlayer(handle, out var player)) return;
        player.ConnectProgress = progress;
    }
}
