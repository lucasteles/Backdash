using Backdash.Core;
using Backdash.Data;
using Backdash.Network.Messages;

namespace Backdash.Network;

sealed class ConnectionStatuses()
{
    public ConnectStatus[] Statuses { get; } = new ConnectStatus[Max.MsgPlayers];
    public int Length => Statuses.Length;

    public ConnectionStatuses(Frame lastFrame) : this()
    {
        for (var i = 0; i < Statuses.Length; i++)
            Statuses[i].LastFrame = lastFrame;
    }

    public ref ConnectStatus this[QueueIndex queue] => ref Statuses[queue.Number];
    public ref ConnectStatus this[int index] => ref Statuses[index];
}
