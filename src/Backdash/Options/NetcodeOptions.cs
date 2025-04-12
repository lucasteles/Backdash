using Backdash.Core;
using Backdash.Network;
using Backdash.Network.Client;
using Backdash.Synchronizing.State;

namespace Backdash.Options;

/// <summary>
///     Configurations for sessions.
/// </summary>
/// <seealso cref="RollbackNetcode" />
/// <seealso cref="INetcodeSession{TInput}" />
public sealed record NetcodeOptions
{
    /// <summary>
    ///     Local Port for UDP connections
    /// </summary>
    /// <seealso cref="UdpSocket" />
    /// <value>Defaults to <c>random port</c></value>
    public int LocalPort { get; set; }

    /// <summary>
    ///     Number of players
    ///     Can not be greater than <see cref="Max.NumberOfPlayers" />
    /// </summary>
    /// <value>Defaults to <c>2</c></value>
    public int NumberOfPlayers { get; set; } = 2;

    /// <summary>
    ///     Interval for time synchronization notifications.
    /// </summary>
    /// <seealso cref="TimeSync" />
    /// <seealso cref="TimeSyncOptions" />
    /// <value>Defaults to <c>240</c> milliseconds</value>
    public int RecommendationInterval { get; set; } = 240;

    /// <summary>
    ///     Sets the <see cref="Endianness" /> used for state serialization.
    ///     If null, use the same endianness as <see cref="ProtocolOptions" />.
    ///     <see cref="ProtocolOptions.SerializationEndianness" /> will be used.
    /// </summary>
    /// <seealso cref="Platform" />
    /// <value>Defaults to <see cref="Endianness.LittleEndian" /></value>
    public Endianness? StateSerializationEndianness { get; set; } = Endianness.LittleEndian;

    internal Endianness GetStateSerializationEndianness() =>
        StateSerializationEndianness ?? Protocol.SerializationEndianness;

    /// <summary>
    ///     Max length for player input queues.
    /// </summary>
    /// <value>Defaults to <c>128</c></value>
    public int InputQueueLength { get; set; } = 128;

    /// <summary>
    ///     Max length for spectators input queues.
    /// </summary>
    /// <value>Defaults to <see cref="InputQueueLength" /></value>
    public int SpectatorInputBufferLength { get; set; }

    /// <summary>
    ///     Max allowed prediction frames.
    /// </summary>
    /// <seealso cref="ResultCode.PredictionThreshold" />
    /// <value>Defaults to <c>16</c></value>
    public int PredictionFrames { get; set; } = 16;

    /// <summary>
    ///     Value to be incremented on <see cref="PredictionFrames" /> in state store.
    /// </summary>
    /// <value>Defaults to <c>2</c></value>
    /// <seealso cref="IStateStore.Initialize" />
    /// <seealso cref="IStateStore" />
    public int PredictionFramesOffset { get; set; } = 2;

    /// <summary>
    ///     Amount of frames to delay local input.
    /// </summary>
    /// <value>Defaults to <c>2</c></value>
    public int InputDelayFrames { get; set; } = 2;

    /// <summary>
    ///     Value to override the total number of <see cref="SavedFrame" /> in state store.
    /// </summary>
    /// <value>Defaults to <see cref="PredictionFrames" /> + <see cref="PredictionFramesOffset"/></value>
    /// <seealso cref="IStateStore" />
    public int SaveStateCount { get; set; }

    internal int TotalSavedFramesAllowed => Math.Max(PredictionFrames + PredictionFramesOffset, SaveStateCount);

    /// <summary>
    ///     Size hint in bytes for state serialization pre-allocation.
    /// </summary>
    /// <value>Defaults to <c>512</c> bytes</value>
    public int StateSizeHint { get; set; } = 512;

    /// <summary>
    ///     Config <see cref="UdpSocket" /> to use IPv6.
    /// </summary>
    /// <value>Defaults to <see langword="false" /></value>
    public bool UseIPv6 { get; set; }

    /// <summary>
    ///     Base frame rate used to estimate fairness (frame advantage) over peers.
    ///     <inheritdoc cref="FrameTime.DefaultFrameRate" />
    /// </summary>
    /// <seealso cref="FrameTime"/>
    public int FrameRate { get; set; } = FrameTime.DefaultFrameRate;

    /// <summary>Time synchronization options.</summary>
    /// <seealso cref="TimeSyncOptions" />
    public TimeSyncOptions TimeSync { get; set; } = new();

    /// <summary>Logging options.</summary>
    /// <seealso cref="LoggerOptions" />
    public LoggerOptions Logger { get; set; } = new();

    /// <summary>Networking Protocol options.</summary>
    /// <seealso cref="ProtocolOptions" />
    public ProtocolOptions Protocol { get; set; } = new();

    /// <summary>Run jobs in a separated thread.</summary>
    /// <value>Defaults to <c>true</c></value>
    public bool UseBackgroundThread { get; set; } = true;

    internal void EnsureDefaults()
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(NumberOfPlayers);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(NumberOfPlayers, Max.NumberOfPlayers);

        if (Protocol.UdpPacketBufferSize <= 0)
            Protocol.UdpPacketBufferSize = Max.NumberOfPlayers * Max.CompressedBytes * 2;

        if (SpectatorInputBufferLength <= 0)
            SpectatorInputBufferLength = InputQueueLength;

        if (LocalPort < 0)
            LocalPort = NetUtils.FindFreePort();
    }

    internal NetcodeOptions CloneOptions() =>
        this with
        {
            TimeSync = TimeSync with { },
            Protocol = Protocol with { },
            Logger = Logger with { },
        };
}
