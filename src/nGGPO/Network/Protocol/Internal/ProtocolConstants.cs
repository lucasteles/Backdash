namespace nGGPO.Network.Protocol.Internal;

static class ProtocolConstants
{
    public const int UdpHeaderSize = 28; /* Size of IP + UDP headers */
    public const int SyncRetryInterval = 2000;
    public const int SyncFirstRetryInterval = 500;
    public const int RunningRetryInterval = 200;
    public const int KeepAliveInterval = 200;
    public const int QualityReportInterval = 1000;
    public const int NetworkStatsInterval = 1000;
    public const long UdpShutdownTimer = 5000;
    public const int MaxSeqDistance = 1 << 15;
}
