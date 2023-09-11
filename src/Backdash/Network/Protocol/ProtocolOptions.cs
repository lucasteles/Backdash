using Backdash.Core;
using Backdash.Data;

namespace Backdash.Network.Protocol;

class ProtocolOptions
{
    public int MaxInputQueue { get; set; } = Max.InputQueue;
    public required QueueIndex Queue { get; init; }
    public required int NetworkDelay { get; init; }
    public required Peer Peer { get; init; }
    public required int DisconnectTimeout { get; init; }
    public required int DisconnectNotifyStart { get; init; }
    public required int UdpPacketBufferSize { get; init; }

    public int NumberOfSyncPackets { get; init; } = 5;
    public long UdpShutdownTimer { get; init; } = 5000;
    public int MaxSeqDistance { get; init; } = 1 << 15;
    public int UdpHeaderSize { get; init; } = 28; /* Size of IP + UDP headers */
    public int SyncRetryInterval { get; init; } = 2000;
    public int SyncFirstRetryInterval { get; init; } = 500;
    public int RunningRetryInterval { get; init; } = 200;
    public int KeepAliveInterval { get; init; } = 200;
    public int QualityReportInterval { get; init; } = 1000;
    public int NetworkStatsInterval { get; init; } = 1000;
}
