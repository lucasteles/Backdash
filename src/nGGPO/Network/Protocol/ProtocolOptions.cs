using nGGPO.Data;
using nGGPO.Utils;

namespace nGGPO.Network.Protocol;

class ProtocolOptions
{
    public int MaxInputQueue { get; set; } = Max.InputQueue;
    public required QueueIndex Queue { get; init; }
    public required int NetworkDelay { get; init; }
    public required Peer Peer { get; init; }
    public required int DisconnectTimeout { get; init; }
    public required int DisconnectNotifyStart { get; init; }
    public required int NumberOfSyncPackets { get; init; }
}
