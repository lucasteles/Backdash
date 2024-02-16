using Backdash.Data;

namespace Backdash.Network;

public sealed class RollbackSessionInfo
{
    public TimeSpan Ping { get; internal set; }
    public int LocalFrameBehind { get; internal set; }
    public int RemoteFrameBehind { get; internal set; }
    public int PendingInputCount { get; internal set; }
    public TimeSpan LastReceivedTime { get; internal set; }
    public int LastAckedFrame { get; internal set; }
    public int LastSendFrame { get; internal set; }
    public int CurrentFrame { get; internal set; }
    public ByteSize BytesSent { get; internal set; }
    public int PacketsSent { get; internal set; }
    public float Pps { get; internal set; }
    public float UdpOverhead { get; internal set; }
    public float BandwidthKbps { get; internal set; }
    public ByteSize TotalBytesSent { get; internal set; }
    public TimeSpan LastSendTime { get; internal set; }
    public int RollbackFrames { get; internal set; }
}
