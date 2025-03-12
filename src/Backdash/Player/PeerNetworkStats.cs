using Backdash.Data;
using Backdash.Options;

namespace Backdash;

/// <summary>
/// Holds current session network stats.
/// Calculated in intervals of <see cref="ProtocolOptions.NetworkStatsInterval"/>.
/// </summary>
public sealed class PeerNetworkStats
{
    /// <summary>Current roundtrip ping time</summary>
    public TimeSpan Ping { get; internal set; }

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

    /// <summary>
    /// Hold package traffic data
    /// </summary>
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
    }
}
