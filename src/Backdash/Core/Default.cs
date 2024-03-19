namespace Backdash.Core;

static class Default
{
    ///<value>Defaults to <c>1000</c></value>
    public const int SpectatorOffset = 1000;

    ///<value>Defaults to <c>240</c> milliseconds</value>
    public const int RecommendationInterval = 240;

    ///<value>Defaults to <c>4096</c></value>
    public const int UdpPacketBufferSize = Max.CompressedBytes * Max.NumberOfPlayers * 2;

    ///<value>Defaults to <c>2</c></value>
    public const int PredictionFramesOffset = 2;

    ///<value>Defaults to <c>16</c></value>
    public const int PredictionFrames = 16;

    ///<value>Defaults to <c>750</c> milliseconds</value>
    public const int DisconnectNotifyStart = 750;

    ///<value>Defaults to <c>5_000</c> milliseconds</value>
    public const int DisconnectTimeout = 5_000;

    ///<value>Defaults to <c>128</c></value>
    public const int InputQueueLength = 128;

    ///<value>Defaults to <c>64</c></value>
    public const int MaxPendingInputs = 64;

    ///<value>Defaults to <c>64</c></value>
    public const int MaxPackageQueue = 64;

    public const int NumberOfSyncPackets = 5;

    ///<value>Defaults to <c>100</c> milliseconds</value>
    public const long UdpShutdownTime = 100;

    ///<value>Defaults to <c>32_768</c></value>
    public const int MaxSeqDistance = 1 << 15;

    ///<value>Defaults to <c>1000</c> milliseconds</value>
    public const int SyncRetryInterval = 1000;

    ///<value>Defaults to <c>500</c> milliseconds</value>
    public const int SyncFirstRetryInterval = 500;

    ///<value>Defaults to <c>200</c> milliseconds</value>
    public const int KeepAliveInterval = 200;

    ///<value>Defaults to <c>1000</c> milliseconds</value>
    public const int QualityReportInterval = 1000;

    ///<value>Defaults to <c>1000</c> milliseconds</value>
    public const int NetworkStatsInterval = 1000;

    ///<value>Defaults to <c>2</c></value>
    public const int FrameDelay = 2;

    ///<value>Defaults to <c>200</c> milliseconds</value>
    public const int ResendInputInterval = 200;

    ///<value>Defaults to <c>64</c></value>
    public const int MaxSyncRetries = 64;
}
