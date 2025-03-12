using Backdash.Core;
using Backdash.Data;
using Backdash.Network;
using Backdash.Network.Client;
using Backdash.Network.Protocol;
using Backdash.Synchronizing;
using Backdash.Synchronizing.State;

namespace Backdash;

/// <summary>
/// Configurations for sessions.
/// </summary>
///  <seealso cref="RollbackNetcode"/>
///  <seealso cref="INetcodeSession{TInput}"/>
public sealed record NetcodeOptions
{
    /// <summary>
    /// Offset to be incremented to spectators <see cref="PlayerHandle.Number"/> when added to session.
    /// </summary>
    /// <seealso cref="PlayerType.Spectator"/>
    /// <seealso cref="INetcodeSession{TInput}.AddPlayer"/>
    /// <inheritdoc cref="Default.SpectatorOffset"/>
    public int SpectatorOffset { get; set; } = Default.SpectatorOffset;

    /// <summary>
    /// Interval for time synchronization notifications.
    /// </summary>
    /// <seealso cref="TimeSync"/>
    /// <seealso cref="TimeSyncOptions"/>
    /// <inheritdoc cref="Default.RecommendationInterval"/>
    public int RecommendationInterval { get; set; } = Default.RecommendationInterval;

    /// <summary>
    /// Sets the <see cref="Endianness"/> used for state serialization.
    /// If null, use the same endianness as <see cref="ProtocolOptions"/>.<see cref="ProtocolOptions.SerializationEndianness"/> will be used.
    /// </summary>
    /// <seealso cref="Platform"/>
    /// <value>Defaults to <see cref="Endianness.LittleEndian"/></value>
    public Endianness? StateSerializationEndianness { get; set; } = Endianness.LittleEndian;

    internal Endianness GetStateSerializationEndianness() =>
        StateSerializationEndianness ?? Protocol.SerializationEndianness;

    /// <summary>
    /// Max length for player input queues.
    /// </summary>
    /// <inheritdoc cref="Default.InputQueueLength"/>
    public int InputQueueLength { get; set; } = Default.InputQueueLength;

    /// <summary>
    /// Max length for spectators input queues.
    /// </summary>
    /// <inheritdoc cref="Default.InputQueueLength"/>
    public int SpectatorInputBufferLength { get; set; } = Default.InputQueueLength;

    /// <summary>
    /// Max allowed prediction frames.
    /// </summary>
    /// <seealso cref="ResultCode.PredictionThreshold"/>
    /// <inheritdoc cref="Default.PredictionFrames"/>
    public int PredictionFrames { get; set; } = Default.PredictionFrames;

    /// <summary>
    /// Value to be incremented on <see cref="PredictionFrames"/> in state store.
    /// <see cref="IStateStore.Initialize"/>
    /// </summary>
    /// <inheritdoc cref="Default.PredictionFramesOffset"/>
    /// <seealso cref="IStateStore"/>
    public int PredictionFramesOffset { get; set; } = Default.PredictionFramesOffset;

    /// <summary>
    /// Total allowed prediction frames.
    /// </summary>
    internal int TotalPredictionFrames => PredictionFrames + PredictionFramesOffset;

    /// <summary>
    /// Amount of frames to delay for local input.
    /// </summary>
    /// <inheritdoc cref="Default.FrameDelay"/>
    public int FrameDelay { get; set; } = Default.FrameDelay;

    /// <summary>
    /// Size hint in bytes for state serialization pre-allocation.
    /// </summary>
    /// <inheritdoc cref="Default.StateSizeHint"/>
    public int StateSizeHint { get; set; } = Default.StateSizeHint;

    /// <summary>
    /// Config <see cref="UdpSocket"/> to use IPv6.
    /// </summary>
    /// <value>Defaults to <see langword="false"/></value>
    public bool UseIPv6 { get; set; }

    /// <summary>
    /// Base FPS used to estimate fairness (frame advantage) over peers.
    /// </summary>
    /// <inheritdoc cref="FrameSpan.DefaultFramesPerSecond"/>
    public short FramesPerSecond { get; set; } = FrameSpan.DefaultFramesPerSecond;

    /// <summary>Time synchronization options.</summary>
    /// <seealso cref="TimeSyncOptions"/>
    public TimeSyncOptions TimeSync { get; set; } = new();

    /// <summary>Logging options.</summary>
    /// <seealso cref="LoggerOptions"/>
    public LoggerOptions Logger { get; set; } = new();

    /// <summary>Networking Protocol options.</summary>
    /// <seealso cref="ProtocolOptions"/>
    public ProtocolOptions Protocol { get; set; } = new();
}
