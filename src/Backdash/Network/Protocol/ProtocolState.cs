using Backdash.Core;
using Backdash.Data;

namespace Backdash.Network.Protocol;

sealed class ProtocolState(
    PlayerHandle player,
    Peer peer,
    ConnectionsState localConnectStatuses
)
{
    public readonly CancellationTokenSource StoppingTokenSource = new();
    public CancellationToken StoppingToken => StoppingTokenSource.Token;

    public readonly PlayerHandle Player = player;
    public readonly Peer Peer = peer;

    public readonly SyncState Sync = new();
    public readonly ConnectionState Connection = new();
    public readonly AdvantageState Fairness = new();
    public readonly Statistics Stats = new();

    public readonly ConnectionsState LocalConnectStatuses = localConnectStatuses;
    public readonly ConnectionsState PeerConnectStatuses = new(Max.RemoteConnections, Frame.Null);
    public ProtocolStatus CurrentStatus;

    public sealed class ConnectionState
    {
        public bool DisconnectEventSent;
        public bool DisconnectNotifySent;
        public bool IsConnected;
    }

    public sealed class AdvantageState
    {
        public Frame LocalFrameAdvantage;
        public Frame RemoteFrameAdvantage;
    }

    public sealed class Statistics
    {
        public TimeSpan RoundTripTime;
        public long LastInputPacketRecvTime;
        public long LastSendTime;
        public ByteSize BytesSent;
        public int PacketsSent;
        public float Pps;
        public float UdpOverhead;
        public float BandwidthKbps;
        public ByteSize TotalBytesSent;
    }

    public sealed class SyncState
    {
        public readonly object Locker = new();

        uint remainingRoundtrips;
        uint currentRandom;
        TimeSpan totalRoundtripsPing;

        public uint CurrentRandom
        {
            get
            {
                lock (Locker) return currentRandom;
            }
            set
            {
                lock (Locker) currentRandom = value;
            }
        }

        public TimeSpan TotalRoundtripsPing
        {
            get
            {
                lock (Locker) return totalRoundtripsPing;
            }
            set
            {
                lock (Locker) totalRoundtripsPing = value;
            }
        }

        public uint RemainingRoundtrips
        {
            get
            {
                lock (Locker)
                {
                    return remainingRoundtrips;
                }
            }
            set
            {
                lock (Locker)
                {
                    remainingRoundtrips = value;
                }
            }
        }
    }
}

enum ProtocolStatus
{
    Syncing,
    Running,
    Disconnected,
}
