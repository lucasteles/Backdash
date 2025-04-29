using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Backdash.Data;
using Backdash.Network.Protocol;
using Backdash.Options;

namespace Backdash;

/// <summary>
///     Holds current session network stats.
///     Calculated in intervals of <see cref="ProtocolOptions.NetworkPackageStatsInterval" />.
/// </summary>
[Serializable]
public sealed class PeerNetworkStats
{
    /// <summary>
    ///     Returns true if the last read stats call was successful.
    /// </summary>
    [MemberNotNullWhen(true, nameof(Session))]
    public bool Valid { get; internal set; }

    /// <summary>Current roundtrip ping time</summary>
    public TimeSpan Ping { get; internal set; }

    /// <summary>Current session info</summary>
    public INetcodeSessionInfo? Session { get; internal set; }

    /// <summary>Remote frame advantage</summary>
    public FrameSpan LocalFramesBehind { get; internal set; }

    /// <summary>Local frame advantage</summary>
    public FrameSpan RemoteFramesBehind { get; internal set; }

    /// <summary>Number of pending queued inputs</summary>
    public int PendingInputCount { get; internal set; }

    /// <summary>Last acknowledged frame</summary>
    public Frame LastAckedFrame { get; internal set; }

    /// <summary>Packages sent info</summary>
    public PackagesInfo Send { get; } = new();

    /// <summary>Packages received info</summary>
    public PackagesInfo Received { get; } = new();

    /// <summary>Returns session rollback frames.</summary>
    public FrameSpan? RollbackFrames => Session?.RollbackFrames;

    /// <summary>
    ///     Hold package traffic data
    /// </summary>
    [Serializable]
    public sealed class PackagesInfo
    {
        /// <summary>Last package time</summary>
        public TimeSpan LastTime { get; internal set; }

        /// <summary>Total transferred bytes</summary>
        public ByteSize TotalBytes { get; internal set; }

        /// <summary>Number of packages</summary>
        public int Count { get; internal set; }

        /// <summary>Packages per second</summary>
        public float PackagesPerSecond { get; internal set; }

        /// <summary>Last package frame</summary>
        public Frame LastFrame { get; internal set; }

        /// <summary>Total used bandwidth</summary>
        public ByteSize Bandwidth { get; internal set; }

        /// <summary>Reset all values to default</summary>
        public void Reset()
        {
            LastTime = TimeSpan.Zero;
            TotalBytes = ByteSize.Zero;
            Count = 0;
            PackagesPerSecond = 0;
            LastFrame = Frame.Zero;
            Bandwidth = ByteSize.Zero;
        }

        internal void Fill(ProtocolState.PackagesStats stats)
        {
            TotalBytes = stats.TotalBytesWithHeaders;
            Count = stats.TotalPackets;
            LastTime = Stopwatch.GetElapsedTime(stats.LastTime);
            PackagesPerSecond = stats.PackagesPerSecond;
            Bandwidth = stats.Bandwidth;
        }
    }

    /// <summary>Reset all values to default</summary>
    public void Reset()
    {
        Valid = false;
        Session = null;
        Ping = TimeSpan.Zero;
        LocalFramesBehind = FrameSpan.Zero;
        RemoteFramesBehind = FrameSpan.Zero;
        PendingInputCount = 0;
        LastAckedFrame = Frame.Zero;
        Send.Reset();
        Received.Reset();
    }
}
