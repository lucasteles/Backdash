namespace Backdash.Core;

static class Default
{
    public const int SpectatorOffset = 1000;
    public const int RecommendationInterval = 240;
    public const int DisconnectNotifyStart = 750;
    public const int UdpPacketBufferSize = Max.CompressedBytes * Max.RemoteConnections * 2;
    public const int PredictionFramesOffset = 2;
    public const int PredictionFrames = 32;
    public const int DisconnectTimeout = 5_000;

    public const int MaxInputQueue = Max.CompressedBytes / Max.TotalInputSizeInBytes;
    public const int MaxPackageQueue = 128;
    public const int NumberOfSyncPackets = 5;
    public const long UdpShutdownTime = 100;
    public const int MaxSeqDistance = 1 << 15;
    public const int SyncRetryInterval = 2000;
    public const int SyncFirstRetryInterval = 500;
    public const int KeepAliveInterval = 200;
    public const int QualityReportInterval = 1000;
    public const int NetworkStatsInterval = 1000;
    public const int FrameDelay = 2;
    public const int ResendInputInterval = 200;
}
