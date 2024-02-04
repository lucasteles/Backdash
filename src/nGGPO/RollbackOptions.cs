using nGGPO.Core;
using nGGPO.Input;

namespace nGGPO;

public sealed class RollbackOptions
{
    public LogLevel LogLevel { get; init; } =
#if DEBUG
        LogLevel.Trace;
#else
        LogLevel.Error;
#endif

    public Random Random { get; init; } = Random.Shared;
    public required int LocalPort { get; init; }
    public int NumberOfPlayers { get; init; }
    public int NumberOfSpectators { get; init; } = Max.Spectators;
    public int SpectatorOffset { get; init; } = 1000;

    public int RecommendationInterval { get; init; } = 240;
    public int DisconnectTimeout { get; init; } = 5000;
    public int DisconnectNotifyStart { get; init; } = 750;
    public int UdpPacketBufferSize { get; init; } = Max.CompressedBytes * Max.MsgPlayers;
    public int NetworkDelay { get; init; }
    public bool EnableEndianness { get; init; }

    public TimeSyncOptions TimeSync { get; init; } = new();

    internal int InputSize { get; set; }

    public int PredictionFrames { get; init; } = Max.PredictionFrames;
    public int PredictionFramesOffset { get; init; } = 2;
    public int InputQueueLength { get; init; } = 128;
}
