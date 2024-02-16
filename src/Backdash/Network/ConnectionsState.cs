using Backdash.Data;
using Backdash.Network.Messages;

namespace Backdash.Network;

sealed class ConnectionsState
{
    public int Length => Statuses.Length;
    public readonly ConnectStatus[] Statuses;

    public ConnectionsState(int size, Frame lastFrame)
    {
        Statuses = new ConnectStatus[size];
        for (var i = 0; i < Statuses.Length; i++)
            Statuses[i].LastFrame = lastFrame;
    }

    public ConnectionsState(int size) : this(size, Frame.Null) { }

    public ref ConnectStatus this[in PlayerHandle player] => ref Statuses[player.Index];
    public ref ConnectStatus this[int index] => ref Statuses[index];

    public bool IsKnown(in PlayerHandle player) => player.Index >= 0 && player.Index < Length;

    public bool IsConnected(in PlayerHandle player) => IsKnown(player) && !this[player].Disconnected;

    public bool AnyConnected()
    {
        for (var i = 0; i < Statuses.Length; i++)
            if (!Statuses[i].Disconnected)
                return true;

        return false;
    }

    public void CopyTo(Span<ConnectStatus> buffer) => Statuses.CopyTo(buffer);
}
