using Backdash.Core;
using Backdash.Network.Protocol;
using Backdash.Sync;

namespace Backdash;

public sealed class RollbackOptions(int port, int numberOfPlayers)
{
    public LogLevel LogLevel { get; init; } =
#if DEBUG
        LogLevel.Debug;
#else
        LogLevel.Error;
#endif

    public int LocalPort { get; } = port;
    public int NumberOfPlayers { get; } = numberOfPlayers;
    public Random Random { get; init; } = Random.Shared;
    public int NumberOfSpectators { get; init; }
    public int SpectatorOffset { get; init; } = Default.SpectatorOffset;
    public int RecommendationInterval { get; init; } = Default.RecommendationInterval;
    public bool EnableEndianness { get; init; } = true;
    public int PredictionFrames { get; init; } = Default.PredictionFrames;
    public int PredictionFramesOffset { get; init; } = Default.PredictionFramesOffset;
    public int FrameDelay { get; init; } = Default.FrameDelay;
    public TimeSyncOptions TimeSync { get; init; } = new();
    public ProtocolOptions Protocol { get; init; } = new();
}
