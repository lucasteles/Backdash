using nGGPO.Data;

namespace nGGPO.Network;

public sealed class PeerConnectionInfo
{
    public int Ping { get; internal set; }
    public int RemoteFrameAdvantage { get; internal set; }
    public int LocalFrameAdvantage { get; internal set; }
    public int LocalFramesBehind { get; internal set; }
    public int RemoteFramesBehind { get; internal set; }
    public int SendQueueLen { get; internal set; }
    public int RecvQueueLen { get; internal set; }

    public ByteSize BytesSent { get; internal set; }
    public int PacketsSent { get; internal set; }
    public float KbpsSent { get; internal set; }
}
