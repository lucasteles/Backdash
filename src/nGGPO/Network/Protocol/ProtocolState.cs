using nGGPO.Data;
using nGGPO.Network.Messages;
using nGGPO.Utils;

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
    public readonly Statistics Stats = new();
    public readonly Peer PeerAddress;

    public readonly ConnectStatus[] LocalConnectStatus;
    public readonly ConnectStatus[] PeerConnectStatus;
    public ProtocolStatus Status;

    public required QueueIndex QueueIndex { get; init; }

    public ProtocolState(Peer peer, ConnectStatus[] localConnectStatus)
    {
        PeerConnectStatus = new ConnectStatus[Max.MsgPlayers];
        for (var i = 0; i < PeerConnectStatus.Length; i++)
            PeerConnectStatus[i].LastFrame = Frame.NullValue;

        LocalConnectStatus = localConnectStatus;
        PeerAddress = peer;
    }
}
