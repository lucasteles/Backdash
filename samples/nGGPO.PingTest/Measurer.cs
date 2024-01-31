using System.Diagnostics;
using System.Text;
using nGGPO.Data;

#pragma warning disable S1215

namespace nGGPO.PingTest;

public sealed class Measurer
{
    public struct MeasureSnapshot()
    {
        public readonly long Timestamp = Stopwatch.GetTimestamp();
        public long Elapsed = 0;
        public long MessageCount = PingMessageHandler.TotalProcessed;
        public ByteSize TotalMemory = (ByteSize) GC.GetTotalMemory(true);
        public ByteSize TotalAllocatedBytes = (ByteSize) GC.GetTotalAllocatedBytes(true);

        public ByteSize AllocatedThreadMemory =
            (ByteSize) GC.GetAllocatedBytesForCurrentThread();

        public TimeSpan PauseTime = GC.GetTotalPauseDuration();
        public int GcCount0 = GC.CollectionCount(0);
        public int GcCount1 = GC.CollectionCount(0);
        public int GcCount2 = GC.CollectionCount(0);

        public static MeasureSnapshot Diff(MeasureSnapshot a, MeasureSnapshot b) => new()
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

        public static MeasureSnapshot Next(MeasureSnapshot last) =>
            Diff(new MeasureSnapshot(), last);

        public readonly override string ToString() =>
            $"""
               TimeStamp: {TimeSpan.FromTicks(Timestamp):c}
               Msg Count: {MessageCount:N0}
               Duration: {TimeSpan.FromTicks(Elapsed).TotalSeconds:F4}s
               GC Pause: {PauseTime.TotalMilliseconds:F}ms
               Collect Count: G1({GcCount0:N0}); G2({GcCount1:N0}); G3({GcCount2:N0})
               Total Memory: {TotalMemory}
               Total Alloc: {TotalAllocatedBytes}
               Thread Alloc: {AllocatedThreadMemory}
             """;
    }

    MeasureSnapshot start;
    readonly List<MeasureSnapshot> snapshots = new(64);
    readonly Stopwatch watch = new();

    public void Start()
    {
        snapshots.Clear();
        GC.Collect();
        start = new();
        watch.Start();
    }

    public void Snapshot() => snapshots.Add(MeasureSnapshot.Next(start));

    public void Stop()
    {
        watch.Stop();
        Snapshot();
    }

    public string Summary(ByteSize totalSent, bool showSnapshots = true)
    {
        StringBuilder builder = new();

        builder.AppendLine(
            $"""
             --- Summary ---
             Duration: {watch.Elapsed:c}
             Snapshots: {snapshots.Count:N0}
             Msg Count: {PingMessageHandler.TotalProcessed:N0}
             Msg Size: {ByteSize.SizeOf<Message>()}
             Avg Msg : {totalSent / PingMessageHandler.TotalProcessed}
             Total Sent: {totalSent}
             """
        );

        if (snapshots is [.., var last])
            builder.AppendLine(
                $"""
                 GC Pause: {last.PauseTime.TotalMilliseconds:F}ms
                 Collect Count: G1({last.GcCount0:N0}); G2({last.GcCount1:N0}); G3({last.GcCount2:N0})
                 Total Memory: {last.TotalMemory}
                 Total Alloc: {last.TotalAllocatedBytes}
                 Thread Alloc: {last.AllocatedThreadMemory}
                 """
            );

        builder.AppendLine();

        if (showSnapshots)
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