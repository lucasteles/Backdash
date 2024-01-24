using System.Net;
using nGGPO.Network.Messages;

namespace nGGPO.Network.Protocol;

partial class UdpProtocol
{
    const int UdpHeaderSize = 28; /* Size of IP + UDP headers */
    const int NumSyncPackets = 5;
    const int SyncRetryInterval = 2000;
    const int SyncFirstRetryInterval = 500;
    const int RunningRetryInterval = 200;
    const int KeepAliveInterval = 200;
    const int QualityReportInterval = 1000;
    const int NetworkStatsInterval = 1000;
    const long UdpShutdownTimer = 5000;
    const int MaxSeqDistance = 1 << 15;

    struct QueueEntry
    {
        public long QueueTime;
        public SocketAddress DestAddr;
        public ProtocolMessage Msg;
    }

    public struct SyncState
    {
        public uint RoundtripsRemaining;
        public uint Random;
    };

    public struct RunningState
    {
        public uint LastQualityReportTime;
        public uint LastNetworkStatsInterval;
        public uint LastInputPacketRecvTime;
    };

    public sealed class UdpProtocolState
    {
        public SyncState Sync = new();
        public RunningState Running = new();
    }
}
