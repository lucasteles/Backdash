using Backdash.Core;
using Backdash.Data;
using Backdash.Serialization.Buffer;

namespace Backdash.Network.Protocol;

sealed class ProtocolState(
    PlayerHandle player,
    Peer peer,
    ConnectionsState localConnectStatuses,
    short fps
)
{
    public readonly CancellationTokenSource StoppingTokenSource = new();
    public CancellationToken StoppingToken => StoppingTokenSource.Token;

    public readonly PlayerHandle Player = player;
    public readonly Peer Peer = peer;

    public readonly SyncState Sync = new();
    public readonly ConnectionState Connection = new();
    public readonly AdvantageState Fairness = new(fps);
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

    public sealed class AdvantageState(short fps)
    {
        public FrameSpan LocalFrameAdvantage;
        public FrameSpan RemoteFrameAdvantage;
        public readonly short FramesPerSecond = fps;
    }

    public class Statistics
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
            writer.Write("UDP Overhead: "u8);
            writer.Write(UdpOverhead);
            writer.Write("}"u8);
            return true;
        }
    }

    public sealed class SyncState
    {
        public readonly object Locker = new();

        int remainingRoundtrips;
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

        public int RemainingRoundtrips
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
