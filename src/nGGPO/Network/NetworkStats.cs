namespace nGGPO.Network;

public class UdpStats
{
    public int BytesSent { get; internal set; }
    public int PacketsSent { get; internal set; }
    public float KbpsSent { get; internal set; }
}

public class ProtocolStats
{
    public int Ping { get; internal set; }
    public int RemoteFrameAdvantage { get; internal set; }
    public int LocalFrameAdvantage { get; internal set; }
    public int SendQueueLen { get; internal set; }
    public UdpStats Udp { get; internal set; } = new();
}

public record NetworkStats
{
    public int SendQueueLen { get; internal set; }
    public int RecvQueueLen { get; internal set; }
    public int Ping { get; internal set; }
    public int KbpsSent { get; internal set; }
    public int LocalFramesBehind { get; internal set; }
    public int RemoteFramesBehind { get; internal set; }
}
