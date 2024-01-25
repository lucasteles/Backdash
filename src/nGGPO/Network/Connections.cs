using nGGPO.Data;
using nGGPO.Network.Messages;
using nGGPO.Utils;

namespace nGGPO.Network;

sealed class Connections
{
    public ConnectStatus[] Statuses { get; } = new ConnectStatus[Max.MsgPlayers];
    public int Length => Statuses.Length;

    public Connections() { }

    public Connections(Frame lastFrame) : this()
    {
        for (var i = 0; i < Statuses.Length; i++)
            Statuses[i].LastFrame = lastFrame;
    }

    public ref ConnectStatus this[QueueIndex queue] => ref Statuses[queue.Value];
    public ref ConnectStatus this[int index] => ref Statuses[index];

    public bool HasStatuses()
    {
        for (var i = 0; i < Statuses.Length; i++)
            if (Statuses[i].LastFrame is Frame.NullValue)
                return false;

        return true;
    }
}
