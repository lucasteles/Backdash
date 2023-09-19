using System.Net;

namespace nGGPO.Network;

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

    enum StateEnum
    {
        Syncing,
        Synchronzied,
        Running,
        Disconnected,
    }

    struct QueueEntry
    {
        public long QueueTime;
        public IPEndPoint DestAddr;
        public UdpMsg Msg;
    }

    struct Packet
    {
        public int SendTime;
        public IPEndPoint DestAddr;
        public UdpMsg? Msg;
    }

    public class SyncState
    {
        public uint RoundtripsRemaining;
        public uint Random;
    };

    public class RunningState
    {
        public uint LastQualityReportTime;
        public uint LastNetworkStatsInterval;
        public uint LastInputPacketRecvTime;
    };

    public sealed class UdpProtocolState
    {
        public SyncState Sync { get; set; } = new();
        public RunningState Running { get; set; } = new();
    }
}