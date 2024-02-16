using Backdash.Core;

namespace Backdash.Network.Protocol;

public class ProtocolOptions
{
    public int NetworkDelay { get; init; }
    public int UdpPacketBufferSize { get; init; } = Default.UdpPacketBufferSize;
    public int MaxInputQueue { get; init; } = Default.MaxInputQueue;
    public int MaxPackageQueue { get; init; } = Default.MaxPackageQueue;
    public int NumberOfSyncPackets { get; init; } = Default.NumberOfSyncPackets;
    public int MaxSeqDistance { get; init; } = Default.MaxSeqDistance;
    public TimeSpan DisconnectNotifyStart { get; init; } = TimeSpan.FromMilliseconds(Default.DisconnectNotifyStart);
    public TimeSpan ShutdownTime { get; init; } = TimeSpan.FromMilliseconds(Default.UdpShutdownTime);
    public TimeSpan DisconnectTimeout { get; init; } = TimeSpan.FromMilliseconds(Default.DisconnectTimeout);
    public TimeSpan SyncRetryInterval { get; init; } = TimeSpan.FromMilliseconds(Default.SyncRetryInterval);
    public TimeSpan SyncFirstRetryInterval { get; init; } = TimeSpan.FromMilliseconds(Default.SyncFirstRetryInterval);
    public TimeSpan KeepAliveInterval { get; init; } = TimeSpan.FromMilliseconds(Default.KeepAliveInterval);
    public TimeSpan QualityReportInterval { get; init; } = TimeSpan.FromMilliseconds(Default.QualityReportInterval);
    public TimeSpan NetworkStatsInterval { get; init; } = TimeSpan.FromMilliseconds(Default.NetworkStatsInterval);
    public TimeSpan ResendInputInterval { get; init; } = TimeSpan.FromMilliseconds(Default.ResendInputInterval);
}
