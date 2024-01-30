using nGGPO.Data;

namespace nGGPO.Network.Protocol.Internal;

sealed class ProtocolState(Connections localConnectStatus)
{
    public readonly SyncState Sync = new();
    public readonly RunningState Running = new();
    public readonly ConnectionState Connection = new();
    public readonly AdvantageState Fairness = new();
    public readonly Statistics Metrics = new();

    public readonly Connections LocalConnectStatus = localConnectStatus;
    public readonly Connections PeerConnectStatus = new(Frame.Null);
    public ProtocolStatus Status;

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
}
