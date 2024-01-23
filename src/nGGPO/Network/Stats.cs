namespace nGGPO.Network;

public record struct UdpStats
(
    int BytesSent,
    int PacketsSent,
    float KbpsSent
);

public record struct ProtocolStats
(
    int Ping,
    int RemoteFrameAdvantage,
    int LocalFrameAdvantage,
    int SendQueueLen,
    UdpStats Udp
);

public record NetworkStats
{
    public int SendQueueLen { get; set; }
    public int RecvQueueLen { get; set; }
    public int Ping { get; set; }
    public int KbpsSent { get; set; }
    public int LocalFramesBehind { get; set; }
    public int RemoteFramesBehind { get; set; }
}
