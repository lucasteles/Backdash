using Backdash.Data;
using Backdash.Options;

namespace Backdash;

/// <summary>
///     Holds current session network stats.
///     Calculated in intervals of <see cref="ProtocolOptions.NetworkStatsInterval" />.
/// </summary>
[Serializable]
public sealed class PeerNetworkStats
{
    /// <summary>
    ///     Returns true if the last read stats call was successful.
    /// </summary>
    public bool Valid { get; internal set; }

    /// <summary>
    ///     Returns the current session <see cref="Frame" />.
    ///     Same as <see cref="INetcodeSessionInfo.CurrentFrame"/>
    /// </summary>
    public Frame CurrentFrame { get; internal set; }

    /// <summary>Current roundtrip ping time</summary>
    public TimeSpan Ping { get; internal set; }

    /// <summary>
    ///     Returns the number of current session rollback frames.
    ///     Same as <see cref="INetcodeSessionInfo.RollbackFrames"/>
    /// </summary>
    public FrameSpan RollbackFrames { get; internal set; }

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
    }

    /// <summary>Reset all values to default</summary>
    public void Reset()
    {
        Valid = false;
        CurrentFrame = Frame.Zero;
        Ping = TimeSpan.Zero;
        RollbackFrames = FrameSpan.Zero;
        LocalFramesBehind = FrameSpan.Zero;
        RemoteFramesBehind = FrameSpan.Zero;
        PendingInputCount = 0;
        LastAckedFrame = Frame.Zero;
        Send.Reset();
        Received.Reset();
    }
}
