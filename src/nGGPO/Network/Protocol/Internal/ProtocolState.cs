using nGGPO.Data;

namespace nGGPO.Network.Protocol;

sealed class ProtocolState
{
    internal class SyncState
    {
        public uint RemainingRoundtrips;
        public uint Random;
    }

    internal class RunningState
    {
        public uint LastQualityReportTime;
        public uint LastNetworkStatsInterval;
        public uint LastInputPacketRecvTime;
    }

    internal class ConnectionState
    {
        public bool DisconnectEventSent;
        public bool DisconnectNotifySent;
        public bool IsConnected;
    }

    internal class AdvantageState
    {
        public int LocalFrameAdvantage;
        public int RemoteFrameAdvantage;
    }

    internal class Statistics
    {
        public int RoundTripTime;
    }

    public readonly SyncState Sync = new();
    public readonly RunningState Running = new();
    public readonly ConnectionState Connection = new();
    public readonly AdvantageState Fairness = new();
    public readonly Statistics Metrics = new();
    public readonly Peer PeerAddress;

    public readonly Connections LocalConnectStatus;
    public readonly Connections PeerConnectStatus;
    public ProtocolStatus Status;

    public required QueueIndex QueueIndex { get; init; }

    public ProtocolState(Peer peer, Connections localConnectStatus)
    {
        PeerConnectStatus = new(Frame.Null);
        LocalConnectStatus = localConnectStatus;
        PeerAddress = peer;
    }
}
