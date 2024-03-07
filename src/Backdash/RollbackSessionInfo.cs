using Backdash.Data;
namespace Backdash;
public sealed class RollbackNetworkStatus
{
    public TimeSpan Ping { get; internal set; }
    public FrameSpan LocalFramesBehind { get; internal set; }
    public FrameSpan RemoteFramesBehind { get; internal set; }
    public int PendingInputCount { get; internal set; }
    public Frame LastAckedFrame { get; internal set; }
    public PackagesInfo Send { get; } = new();
    public PackagesInfo Received { get; } = new();
    public sealed class PackagesInfo
    {
        public TimeSpan LastTime { get; internal set; }
        public ByteSize TotalBytes { get; internal set; }
        public int Count { get; internal set; }
        public float PackagesPerSecond { get; internal set; }
        public Frame LastFrame { get; internal set; }
        public ByteSize Bandwidth { get; internal set; }
    }
}
