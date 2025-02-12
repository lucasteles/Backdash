using Backdash.Core;
using Backdash.Network.Client;

namespace Backdash.Network.Protocol;

/// <summary>
/// Network protocol configuration.
/// </summary>
public class ProtocolOptions
{
    /// <summary>
    /// Number of bytes used on the <see cref="UdpSocket"/> message buffer.
    /// </summary>
    /// <inheritdoc cref="Default.UdpPacketBufferSize"/>
    public int UdpPacketBufferSize { get; init; } = Default.UdpPacketBufferSize;

    /// <summary>
    /// Max allowed pending inputs in sending queue. When reached <see cref="IRollbackSession{TInput}.AddLocalInput"/>
    /// returns <see cref="ResultCode.InputDropped"/>.
    /// </summary>
    /// <inheritdoc cref="Default.MaxPendingInputs"/>
    public int MaxPendingInputs { get; init; } = Default.MaxPendingInputs;

    /// <summary>
    /// Max allowed pending UDP output messages. When reached removes and ignores the oldest package in the queue
    /// in order to make room for the new package.
    /// </summary>
    /// <inheritdoc cref="Default.MaxPackageQueue"/>
    public int MaxPackageQueue { get; init; } = Default.MaxPackageQueue;

    /// <summary>
    /// Number of synchronization roundtrips to consider two clients synchronized.
    /// </summary>
    /// <inheritdoc cref="Default.NumberOfSyncPackets"/>
    public int NumberOfSyncRoundtrips { get; init; } = Default.NumberOfSyncPackets;

    /// <summary>
    /// Distance to check out-of-order packets.
    /// </summary>
    /// <inheritdoc cref="Default.MaxSeqDistance"/>
    public int MaxSequenceDistance { get; init; } = Default.MaxSeqDistance;

    /// <summary>
    /// Total number of synchronization request retries. When reached,
    /// session will dispatch the <see cref="PeerEvent.SynchronizationFailure"/> event.
    /// </summary>
    /// <inheritdoc cref="Default.MaxSyncRetries"/>
    public int MaxSyncRetries { get; init; } = Default.MaxSyncRetries;

    /// <summary>
    /// Forced network packet sending latency for the current peer.
    /// This value is processed using <see cref="DelayStrategy"/>.
    /// </summary>
    /// <value>Defaults to <see cref="TimeSpan.Zero"/></value>
    /// <seealso cref="Backdash.Network.DelayStrategy"/>
    public TimeSpan NetworkLatency { get; init; }

    /// <summary>
    /// Strategy for applying delay to sending packages, forcing latency.
    /// When <see cref="NetworkLatency"/> is <see cref="TimeSpan.Zero"/> this is ignored.
    /// </summary>
    /// <value>Defaults to <see cref="DelayStrategy.Gaussian"/></value>
    /// <seealso cref="NetworkLatency"/>
    /// <seealso cref="Backdash.Network.DelayStrategy"/>
    public DelayStrategy DelayStrategy { get; init; } = DelayStrategy.Gaussian;

    /// <summary>
    /// When true, session log network stats periodically.
    /// </summary>
    /// <value>Defaults to <see lanword="false"/></value>
    public bool LogNetworkStats { get; init; }

    /// <summary>
    /// The time to wait before the first <see cref="PeerEvent.ConnectionInterrupted"/> timeout will be sent.
    /// </summary>
    /// <inheritdoc cref="Default.DisconnectNotifyStart"/>
    public TimeSpan DisconnectNotifyStart { get; init; } = TimeSpan.FromMilliseconds(Default.DisconnectNotifyStart);

    /// <summary>
    /// The session will automatically disconnect from a remote peer if it has not received a packet in the timeout window.
    /// You will be notified of the disconnect via <see cref="PeerEvent.Disconnected"/> event.
    /// </summary>
    /// <inheritdoc cref="Default.DisconnectTimeout"/>
    public TimeSpan DisconnectTimeout { get; init; } = TimeSpan.FromMilliseconds(Default.DisconnectTimeout);

    /// <summary>
    /// The time to wait before end the session.
    /// </summary>
    /// <inheritdoc cref="Default.UdpShutdownTime"/>
    public TimeSpan ShutdownTime { get; init; } = TimeSpan.FromMilliseconds(Default.UdpShutdownTime);

    /// <summary>
    /// The time to wait before resend synchronization retries after the first.
    /// </summary>
    /// <inheritdoc cref="Default.SyncRetryInterval"/>
    /// <seealso cref="SyncFirstRetryInterval"/>
    public TimeSpan SyncRetryInterval { get; init; } = TimeSpan.FromMilliseconds(Default.SyncRetryInterval);

    /// <summary>
    /// The time to wait before resend the first synchronization request retry.
    /// </summary>
    /// <inheritdoc cref="Default.SyncFirstRetryInterval"/>
    /// <seealso cref="SyncRetryInterval"/>
    public TimeSpan SyncFirstRetryInterval { get; init; } = TimeSpan.FromMilliseconds(Default.SyncFirstRetryInterval);

    /// <summary>
    /// When the time from the last send package until now is greater than this, sends a keep alive packets.
    /// </summary>
    /// <inheritdoc cref="Default.KeepAliveInterval"/>
    public TimeSpan KeepAliveInterval { get; init; } = TimeSpan.FromMilliseconds(Default.KeepAliveInterval);

    /// <summary>
    /// The time to wait before send the next quality report package (determines ping).
    /// </summary>
    /// <inheritdoc cref="Default.QualityReportInterval"/>
    public TimeSpan QualityReportInterval { get; init; } = TimeSpan.FromMilliseconds(Default.QualityReportInterval);

    /// <summary>
    /// The time to wait before recalculate network statistics.
    /// </summary>
    /// <inheritdoc cref="Default.NetworkStatsInterval"/>
    /// <seealso cref="PeerNetworkStats"/>
    public TimeSpan NetworkStatsInterval { get; init; } = TimeSpan.FromMilliseconds(Default.NetworkStatsInterval);

    /// <summary>
    /// When the time from the last send input until now is greater than this, resends pending inputs.
    /// </summary>
    /// <inheritdoc cref="Default.ResendInputInterval"/>
    public TimeSpan ResendInputInterval { get; init; } = TimeSpan.FromMilliseconds(Default.ResendInputInterval);

    /// <summary>
    /// Offset to be applied to frame on checksum consistency check.
    /// The sent frame is (<c>LastReceivedFrame - ConsistencyCheckOffset</c>).
    /// </summary>
    /// <inheritdoc cref="Default.ConsistencyCheckOffset"/>
    /// <seealso cref="ConsistencyCheckTimeout"/>
    /// <seealso cref="ConsistencyCheckInterval"/>
    public int ConsistencyCheckOffset { get; init; } = Default.ConsistencyCheckOffset;

    /// <summary>
    /// The time to wait before send next consistency check (0 to disable).
    /// On each interval one peer requests a frame to other peer which must respond
    /// with the state checksum of that frame.
    /// </summary>
    /// <inheritdoc cref="Default.ConsistencyCheckInterval"/>
    /// <seealso cref="ConsistencyCheckOffset"/>
    /// <seealso cref="ConsistencyCheckTimeout"/>
    public TimeSpan ConsistencyCheckInterval { get; init; } =
        TimeSpan.FromMilliseconds(Default.ConsistencyCheckInterval);

    /// <summary>
    /// Max wait time for non-success consistency checks (0 to disable)
    /// </summary>
    /// <inheritdoc cref="Default.ConsistencyCheckTimeout"/>
    /// <seealso cref="ConsistencyCheckOffset"/>
    /// <seealso cref="ConsistencyCheckInterval"/>
    public TimeSpan ConsistencyCheckTimeout { get; init; } =
        TimeSpan.FromMilliseconds(Default.ConsistencyCheckTimeout);
}
