using Backdash.Core;
using Backdash.Network.Client;

namespace Backdash.Network.Protocol;

/// <summary>
/// Network protocol configuration.
/// </summary>
public class ProtocolOptions
{
    /// <summary>
    /// Sets the <see cref="Endianness"/> used for network communication.
    /// </summary>
    /// <seealso cref="Platform"/>
    /// <value>Defaults to <see cref="Endianness.BigEndian"/></value>
    public Endianness SerializationEndianness { get; set; } = Endianness.BigEndian;

    /// <summary>
    /// Number of bytes used on the <see cref="UdpSocket"/> message buffer.
    /// </summary>
    /// <inheritdoc cref="Default.UdpPacketBufferSize"/>
    public int UdpPacketBufferSize { get; set; } = Default.UdpPacketBufferSize;

    /// <summary>
    /// Max allowed pending inputs in sending queue.
    /// When reached <see cref="INetcodeSession{TInput}.AddLocalInput"/> will return <see cref="ResultCode.InputDropped"/>.
    /// </summary>
    /// <inheritdoc cref="Default.MaxPendingInputs"/>
    public int MaxPendingInputs { get; set; } = Default.MaxPendingInputs;

    /// <summary>
    /// Max allowed pending UDP output messages.
    /// When reached removes and ignores the oldest package in the queue in order to make room for the new package.
    /// </summary>
    /// <inheritdoc cref="Default.MaxPackageQueue"/>
    public int MaxPackageQueue { get; set; } = Default.MaxPackageQueue;

    /// <summary>
    /// Number of synchronization roundtrips to consider two clients synchronized.
    /// </summary>
    /// <inheritdoc cref="Default.NumberOfSyncPackets"/>
    public int NumberOfSyncRoundtrips { get; set; } = Default.NumberOfSyncPackets;

    /// <summary>
    /// Distance to check out-of-order packets.
    /// </summary>
    /// <inheritdoc cref="Default.MaxSeqDistance"/>
    public int MaxSequenceDistance { get; set; } = Default.MaxSeqDistance;

    /// <summary>
    /// Total number of synchronization request retries.
    /// When reached, session will dispatch the <see cref="PeerEvent.SynchronizationFailure"/> event.
    /// </summary>
    /// <inheritdoc cref="Default.MaxSyncRetries"/>
    public int MaxSyncRetries { get; set; } = Default.MaxSyncRetries;

    /// <summary>
    /// Forced network packet sending latency for the current peer.
    /// This value is processed using <see cref="DelayStrategy"/>.
    /// </summary>
    /// <value>Defaults to <see cref="TimeSpan.Zero"/></value>
    /// <seealso cref="Backdash.Network.DelayStrategy"/>
    public TimeSpan NetworkLatency { get; set; }

    /// <summary>
    /// Strategy for applying delay to sending packages, forcing latency.
    /// When <see cref="NetworkLatency"/> is <see cref="TimeSpan.Zero"/> this is ignored.
    /// </summary>
    /// <value>Defaults to <see cref="DelayStrategy.Gaussian"/></value>
    /// <seealso cref="NetworkLatency"/>
    /// <seealso cref="Backdash.Network.DelayStrategy"/>
    public DelayStrategy DelayStrategy { get; set; } = DelayStrategy.Gaussian;

    /// <summary>
    /// When true, session log network stats periodically.
    /// </summary>
    /// <value>Defaults to <see lanword="false"/></value>
    public bool LogNetworkStats { get; set; }

    /// <summary>
    /// The time to wait before the first <see cref="PeerEvent.ConnectionInterrupted"/> timeout will be sent.
    /// </summary>
    /// <inheritdoc cref="Default.DisconnectNotifyStart"/>
    public TimeSpan DisconnectNotifyStart { get; set; } = TimeSpan.FromMilliseconds(Default.DisconnectNotifyStart);

    /// <summary>
    /// The session will automatically disconnect from a remote peer if it has not received a packet in the timeout window.
    /// You will be notified of the disconnect via <see cref="PeerEvent.Disconnected"/> event.
    /// </summary>
    /// <inheritdoc cref="Default.DisconnectTimeout"/>
    public TimeSpan DisconnectTimeout { get; set; } = TimeSpan.FromMilliseconds(Default.DisconnectTimeout);

    /// <summary>
    /// The time to wait before end the session.
    /// </summary>
    /// <inheritdoc cref="Default.UdpShutdownTime"/>
    public TimeSpan ShutdownTime { get; set; } = TimeSpan.FromMilliseconds(Default.UdpShutdownTime);

    /// <summary>
    /// The time to wait before resend synchronization retries after the first.
    /// </summary>
    /// <inheritdoc cref="Default.SyncRetryInterval"/>
    /// <seealso cref="SyncFirstRetryInterval"/>
    public TimeSpan SyncRetryInterval { get; set; } = TimeSpan.FromMilliseconds(Default.SyncRetryInterval);

    /// <summary>
    /// The time to wait before resend the first synchronization request retry.
    /// </summary>
    /// <inheritdoc cref="Default.SyncFirstRetryInterval"/>
    /// <seealso cref="SyncRetryInterval"/>
    public TimeSpan SyncFirstRetryInterval { get; set; } = TimeSpan.FromMilliseconds(Default.SyncFirstRetryInterval);

    /// <summary>
    /// When the time from the last send package until now is greater than this, sends a keep alive packets.
    /// </summary>
    /// <inheritdoc cref="Default.KeepAliveInterval"/>
    public TimeSpan KeepAliveInterval { get; set; } = TimeSpan.FromMilliseconds(Default.KeepAliveInterval);

    /// <summary>
    /// The time to wait before send the next quality report package (determines ping).
    /// </summary>
    /// <inheritdoc cref="Default.QualityReportInterval"/>
    public TimeSpan QualityReportInterval { get; set; } = TimeSpan.FromMilliseconds(Default.QualityReportInterval);

    /// <summary>
    /// The time to wait before recalculate network statistics.
    /// </summary>
    /// <inheritdoc cref="Default.NetworkStatsInterval"/>
    /// <seealso cref="PeerNetworkStats"/>
    public TimeSpan NetworkStatsInterval { get; set; } = TimeSpan.FromMilliseconds(Default.NetworkStatsInterval);

    /// <summary>
    /// When the time from the last send input until now is greater than this, resends pending inputs.
    /// </summary>
    /// <inheritdoc cref="Default.ResendInputInterval"/>
    public TimeSpan ResendInputInterval { get; set; } = TimeSpan.FromMilliseconds(Default.ResendInputInterval);

    /// <summary>
    /// Offset to be applied to frame on checksum consistency check.
    /// The frame sent is (<c>LastReceivedFrame - ConsistencyCheckOffset</c>).
    /// </summary>
    /// <inheritdoc cref="Default.ConsistencyCheckDistance"/>
    /// <seealso cref="ConsistencyCheckTimeout"/>
    /// <seealso cref="ConsistencyCheckInterval"/>
    public int ConsistencyCheckDistance { get; set; } = Default.ConsistencyCheckDistance;

    /// <summary>
    /// Enable/Disable consistency check.
    /// </summary>
    /// <seealso cref="ConsistencyCheckDistance"/>
    /// <seealso cref="ConsistencyCheckTimeout"/>
    public bool ConsistencyCheckEnabled { get; set; } = true;

    /// <summary>
    /// The time to wait before send next consistency check (0 to disable).
    /// On each interval one peer requests a frame to other peer which must respond
    /// with the state checksum of that frame.
    /// </summary>
    /// <inheritdoc cref="Default.ConsistencyCheckInterval"/>
    /// <seealso cref="ConsistencyCheckDistance"/>
    /// <seealso cref="ConsistencyCheckTimeout"/>
    public TimeSpan ConsistencyCheckInterval { get; set; } =
        TimeSpan.FromMilliseconds(Default.ConsistencyCheckInterval);

    /// <summary>
    /// Max wait time for non-success consistency checks (0 to disable).
    /// </summary>
    /// <inheritdoc cref="Default.ConsistencyCheckTimeout"/>
    /// <seealso cref="ConsistencyCheckDistance"/>
    /// <seealso cref="ConsistencyCheckInterval"/>
    public TimeSpan ConsistencyCheckTimeout { get; set; } =
        TimeSpan.FromMilliseconds(Default.ConsistencyCheckTimeout);
}
