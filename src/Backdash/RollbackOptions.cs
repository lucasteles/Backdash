using Backdash.Core;
using Backdash.Data;
using Backdash.Network;
using Backdash.Network.Protocol;
using Backdash.Sync;

namespace Backdash;

/// <summary>
/// Configurations for sessions.
/// </summary>
///  <seealso cref="RollbackNetcode"/>
///  <seealso cref="IRollbackSession{TInput}"/>
///  <seealso cref="IRollbackSession{TInput,TGameState}"/>
public sealed class RollbackOptions
{
    /// <summary>
    /// Offset to be incremented to spectators <see cref="PlayerHandle.Number"/> when added to session.
    /// </summary>
    /// <seealso cref="PlayerType.Spectator"/>
    /// <seealso cref="IRollbackSession{TInput,TState}.AddPlayer"/>
    /// <inheritdoc cref="Default.SpectatorOffset"/>
    public int SpectatorOffset { get; init; } = Default.SpectatorOffset;

    /// <summary>
    /// Interval for time synchronization notifications.
    /// </summary>
    /// <seealso cref="TimeSync"/>
    /// <seealso cref="TimeSyncOptions"/>
    /// <inheritdoc cref="Default.RecommendationInterval"/>
    public int RecommendationInterval { get; init; } = Default.RecommendationInterval;

    /// <summary>
    /// Forces serialization byte order to network order <see cref="Endianness.BigEndian"/>.
    /// </summary>
    /// <seealso cref="Endianness"/>
    /// <value>Defaults to <see langword="true"/></value>
    public bool NetworkEndianness { get; init; } = true;

    public int PredictionFrames { get; init; } = Default.PredictionFrames;
    public int InputQueueLength { get; init; } = Default.InputQueueLength;
    public int SpectatorInputBufferLength { get; init; } = Default.InputQueueLength;
    public int PredictionFramesOffset { get; init; } = Default.PredictionFramesOffset;
    public int FrameDelay { get; init; } = Default.FrameDelay;
    public bool UseIPv6 { get; init; }
    public short FramesPerSecond { get; init; } = FrameSpan.DefaultFramesPerSecond;
    public LogOptions Log { get; init; } = new();
    public TimeSyncOptions TimeSync { get; init; } = new();
    public ProtocolOptions Protocol { get; init; } = new();
}
