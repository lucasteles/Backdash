namespace nGGPO.Network;

record struct UdpStats
(
    int BytesSent,
    int PacketsSent,
    float KbpsSent
);

record struct ProtocolStats
(
    int Ping,
    int RemoteFrameAdvantage,
    int LocalFrameAdvantage,
    int SendQueueLen,
    UdpStats Udp
);