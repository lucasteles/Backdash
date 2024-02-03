using nGGPO.Core;
using nGGPO.Data;
using nGGPO.Network.Messages;

namespace nGGPO.Network;

sealed class ConnectionStatuses()
{
    public ConnectStatus[] Statuses { get; } = new ConnectStatus[Max.MsgPlayers];
    public int Length => Statuses.Length;

    public ConnectionStatuses(Frame lastFrame) : this()
    {
        for (var i = 0; i < Statuses.Length; i++)
            Statuses[i].LastFrame = lastFrame;
    }

    public ref ConnectStatus this[QueueIndex queue] => ref Statuses[queue.Value];
    public ref ConnectStatus this[int index] => ref Statuses[index];
}
