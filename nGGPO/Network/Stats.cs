namespace nGGPO.Network;

readonly record struct UdpStats
(
    int BytesSent,
    int PacketsSent,
    float KbpsSent
);

readonly record struct ProtocolStats
(
    int Ping,
    int RemoteFrameAdvantage,
    int LocalFrameAdvantage,
    int SendQueueLen,
    UdpStats Udp
);