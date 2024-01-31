using System.Diagnostics;
using System.Text;
using nGGPO.Data;

namespace nGGPO.PingTest;

public sealed class Measurer
{
    public struct MeasureSnapshot()
    {
        public readonly long Timestamp = Stopwatch.GetTimestamp();
        public long Elapsed = 0;
        public ByteSize TotalMemory = (ByteSize) GC.GetTotalMemory(true);
        public ByteSize TotalAllocatedBytes = (ByteSize) GC.GetTotalAllocatedBytes(true);

        public ByteSize AllocatedThreadMemory =
            (ByteSize) GC.GetAllocatedBytesForCurrentThread();

        public TimeSpan PauseTime = GC.GetTotalPauseDuration();
        public int GcCount0 = GC.CollectionCount(0);
        public int GcCount1 = GC.CollectionCount(0);
        public int GcCount2 = GC.CollectionCount(0);

        public static MeasureSnapshot operator -(MeasureSnapshot a, MeasureSnapshot b) => new()
        {
            Elapsed = a.Timestamp - b.Timestamp,
            TotalMemory = a.TotalMemory - b.TotalMemory,
            TotalAllocatedBytes = a.TotalAllocatedBytes - b.TotalAllocatedBytes,
            AllocatedThreadMemory = a.AllocatedThreadMemory - b.AllocatedThreadMemory,
            PauseTime = a.PauseTime - b.PauseTime,
            GcCount0 = a.GcCount0 - b.GcCount0,
            GcCount1 = a.GcCount1 - b.GcCount1,
            GcCount2 = a.GcCount2 - b.GcCount2,
        };

        public static MeasureSnapshot Next(MeasureSnapshot last) => new MeasureSnapshot() - last;


        public readonly override string ToString() =>
            $"""
               TimeStamp: {(int) TimeSpan.FromTicks(Timestamp).TotalMilliseconds}
               Duration: {TimeSpan.FromTicks(Elapsed):g}
               GC Pause: {PauseTime:g}
               Collect Count: G1({GcCount0}); G2({GcCount1}); G3({GcCount2})
               Total Memory: {FormatByteSize(TotalMemory)}
               Total Alloc: {FormatByteSize(TotalAllocatedBytes)}
               Thread Alloc: {FormatByteSize(TotalAllocatedBytes)}
             """;
    }

    static string FormatByteSize(ByteSize size) => $"{size:KB} | {size:MB}";

    MeasureSnapshot start;
    readonly List<MeasureSnapshot> snapshots = new(64);
    readonly Stopwatch watch = new();

    public void Start()
    {
        snapshots.Clear();
        start = new();
        watch.Start();
    }

    public void Snapshot() => snapshots.Add(MeasureSnapshot.Next(start));

    public void Stop()
    {
        watch.Stop();
        Snapshot();
    }

    public string Summary(ByteSize totalSent)
    {
        StringBuilder builder = new();

        builder.AppendLine(
            $"""
             --- Summary ---
             Total Duration: {watch.Elapsed:g}
             Msg Count: {PingMessageHandler.TotalProcessed}
             Total Sent: {FormatByteSize(totalSent)}
             Msg Size: {sizeof(Message)} Bytes
             """
        );
        builder.AppendLine();

        for (var index = 0; index < snapshots.Count; index++)
        {
            var shot = snapshots[index];
            builder.AppendLine($"=== Snapshot #{index + 1}:");
            builder.AppendLine(shot.ToString());
            builder.AppendLine("======");
            builder.AppendLine();
        }

        builder.AppendLine("------------");
        return builder.ToString();
    }
}