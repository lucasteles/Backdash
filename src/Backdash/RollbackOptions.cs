using Backdash.Core;
using Backdash.Data;
using Backdash.Network.Protocol;
using Backdash.Sync;

namespace Backdash;

public sealed class RollbackOptions
{
    internal int LocalPort { get; set; }
    public Random Random { get; init; } = Random.Shared;
    public int SpectatorOffset { get; init; } = Default.SpectatorOffset;
    public int RecommendationInterval { get; init; } = Default.RecommendationInterval;
    public bool NetworkEndianness { get; init; } = true;
    public int PredictionFrames { get; init; } = Default.PredictionFrames;
    public int InputQueueLength { get; init; } = Default.InputQueueLength;
    public int SpectatorInputBufferLength { get; init; } = Default.InputQueueLength;
    public int PredictionFramesOffset { get; init; } = Default.PredictionFramesOffset;
    public int FrameDelay { get; init; } = Default.FrameDelay;
    public bool RequireIdleInput { get; init; }
    public short FramesPerSecond { get; init; } = FrameSpan.DefaultFramesPerSecond;
    public LogOptions Log { get; init; } = new();
    public TimeSyncOptions TimeSync { get; init; } = new();
    public ProtocolOptions Protocol { get; init; } = new();
}
