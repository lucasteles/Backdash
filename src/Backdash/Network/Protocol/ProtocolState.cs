using Backdash.Core;
using Backdash.Data;
using Backdash.Serialization.Internal;

namespace Backdash.Network.Protocol;

sealed class ProtocolState(
    PlayerHandle player,
    PeerAddress peerAddress,
    ConnectionsState localConnectStatuses,
    ushort syncNumber
)
{
    public readonly CancellationTokenSource StoppingTokenSource = new();
    public CancellationToken StoppingToken => StoppingTokenSource.Token;

    public readonly PlayerHandle Player = player;
    public readonly PeerAddress PeerAddress = peerAddress;
    public readonly SyncState Sync = new();
    public readonly ConnectionState Connection = new();
    public readonly ConsistencyState Consistency = new();
    public readonly AdvantageState Fairness = new();
    public readonly Statistics Stats = new();
    public readonly ConnectionsState LocalConnectStatuses = localConnectStatuses;
    public readonly ConnectionsState PeerConnectStatuses = new(Max.NumberOfPlayers, Frame.Null);
    public readonly ushort SyncNumber = syncNumber;
    public ushort RemoteMagicNumber;
    public ProtocolStatus CurrentStatus;

    public sealed class ConnectionState
    {
        public bool DisconnectEventSent;
        public bool DisconnectNotifySent;
        public bool IsConnected;
    }

    public sealed class ConsistencyState
    {
        public long LastCheck;
        public Frame AskedFrame;
        public uint AskedChecksum;
    }

    public sealed class AdvantageState
    {
        public FrameSpan LocalFrameAdvantage;
        public FrameSpan RemoteFrameAdvantage;
    }

    public sealed class Statistics
    {
        public TimeSpan RoundTripTime = TimeSpan.Zero;
        public long LastReceivedInputTime = 0;
        public PackagesStats Send = new();
        public PackagesStats Received = new();
    }

    public struct PackagesStats : IUtf8SpanFormattable
    {
        public long LastTime;
        public int TotalPackets;
        public ByteSize TotalBytes;
        public float PackagesPerSecond;
        public float UdpOverhead;
        public ByteSize Bandwidth;
        public ByteSize TotalBytesWithHeaders;

        public readonly bool TryFormat(
            Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format,
            IFormatProvider? provider
        )
        {
            bytesWritten = 0;
            Utf8StringWriter writer = new(in utf8Destination, ref bytesWritten);
            writer.Write("{Bandwidth: "u8);
            writer.Write(Bandwidth.KibiBytes, "f2");
            writer.Write(" KBps; Packets: "u8);
            writer.Write(TotalPackets);
            writer.Write(" ("u8);
            writer.Write(PackagesPerSecond, "f2");
            writer.Write(" pps); KiB: "u8);
            writer.Write(TotalBytesWithHeaders.KibiBytes, "f2");
            writer.Write("; UDP Overhead: "u8);
            writer.Write(UdpOverhead);
            writer.Write("}"u8);
            return true;
        }
    }

    public sealed class SyncState
    {
        public readonly object Locker = new();
        int remainingRoundTrips;
        uint currentRandom;
        TimeSpan totalRoundTripsPing;

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

        public TimeSpan TotalRoundTripsPing
        {
            get
            {
                lock (Locker) return totalRoundTripsPing;
            }
            set
            {
                lock (Locker) totalRoundTripsPing = value;
            }
        }

        public int RemainingRoundTrips
        {
            get
            {
                lock (Locker) return remainingRoundTrips;
            }
            set
            {
                lock (Locker) remainingRoundTrips = value;
            }
        }
    }
}
