using System.Diagnostics;
using System.Text;
using Backdash.Data;

#pragma warning disable S1215

namespace Backdash.Benchmarks.Ping;

public sealed class Measurer : IAsyncDisposable
{
    MeasureSnapshot start;
    readonly List<MeasureSnapshot> snapshots = new(128);
    readonly Stopwatch watch = new();

    readonly CancellationTokenSource cts = new();
    readonly PeriodicTimer? timer;


    public Measurer(TimeSpan? snapshotInterval = null)
    {
        if (snapshotInterval is null || snapshotInterval == TimeSpan.Zero)
            return;

        timer = new(snapshotInterval.Value);
        _ = Task.Run(TimerLoop, cts.Token);
    }

    async Task TimerLoop()
    {
        if (timer is null) return;
        while (await timer.WaitForNextTickAsync(cts.Token))
            if (watch.IsRunning)
                Snapshot();
    }

    public void Start()
    {
        snapshots.Clear();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        start = new();
        watch.Start();
    }

    readonly object lockObj = new();

    public void Snapshot()
    {
        lock (lockObj)
            snapshots.Add(MeasureSnapshot.Next(
                start,
                snapshots.Count is 0 ? MeasureSnapshot.Zero : snapshots[^1])
            );
    }

    public void Stop()
    {
        watch.Stop();
        Snapshot();
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await cts.CancelAsync();
        }
        finally
        {
            cts.Dispose();
        }

        timer?.Dispose();
    }

    public string Summary(bool showSnapshots = true)
    {
        StringBuilder builder = new();

        builder.AppendLine(
            $"""
             --- Summary ---
             Duration: {watch.Elapsed:c}
             Snapshots: {snapshots.Count:N0}
             Msg Count: {PingMessageHandler.TotalProcessed:N0}
             Msg Size: {ByteSize.SizeOf<PingMessage>()}
             """
        );

        if (snapshots is [.., var last])
        {
            var avgAlloc = (ByteSize)snapshots
                .Select(x => x.DeltaAllocatedBytes.ByteCount)
                .Average();

            builder.AppendLine(
                $"""
                 Total Memory: {last.TotalMemory}
                 Total Alloc: {last.TotalAllocatedBytes}
                 Alloc p/ Msg: {last.TotalAllocatedBytes / PingMessageHandler.TotalProcessed}
                 Avg Alloc: {avgAlloc}
                 Thread Alloc: {last.AllocatedThreadMemory}
                 GC Pause: {last.PauseTime.TotalMilliseconds:F}ms
                 GC Collection: G1({last.GcCount0}) / G2({last.GcCount1}) / G3({last.GcCount2})
                 """);
        }

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

    public struct MeasureSnapshot()
    {
        public long Elapsed = 0;
        public readonly long Timestamp = Stopwatch.GetTimestamp();
        public long MessageCount = PingMessageHandler.TotalProcessed;
        public ByteSize TotalMemory = (ByteSize)GC.GetTotalMemory(true);
        public ByteSize TotalAllocatedBytes = (ByteSize)GC.GetTotalAllocatedBytes(true);
        public readonly int ThreadId = Environment.CurrentManagedThreadId;
        public ByteSize AllocatedThreadMemory = (ByteSize)GC.GetAllocatedBytesForCurrentThread();
        public TimeSpan PauseTime = GC.GetTotalPauseDuration();
        public int GcCount0 = GC.CollectionCount(0);
        public int GcCount1 = GC.CollectionCount(1);
        public int GcCount2 = GC.CollectionCount(2);

        public ByteSize DeltaAllocatedBytes;
        public ByteSize DeltaTotalMemory;

        public static MeasureSnapshot Diff(
            MeasureSnapshot next,
            MeasureSnapshot first,
            MeasureSnapshot previous
        ) => new()
        {
            Elapsed = next.Timestamp - first.Timestamp,
            TotalMemory = next.TotalMemory - first.TotalMemory,
            TotalAllocatedBytes = next.TotalAllocatedBytes - first.TotalAllocatedBytes,
            AllocatedThreadMemory = next.ThreadId == first.ThreadId
                ? next.AllocatedThreadMemory - first.AllocatedThreadMemory
                : first.AllocatedThreadMemory,
            PauseTime = next.PauseTime - first.PauseTime,
            GcCount0 = next.GcCount0 - first.GcCount0,
            GcCount1 = next.GcCount1 - first.GcCount1,
            GcCount2 = next.GcCount2 - first.GcCount2,

            DeltaAllocatedBytes =
                next.TotalAllocatedBytes - previous.TotalAllocatedBytes - first.TotalAllocatedBytes,

            DeltaTotalMemory = next.TotalMemory - previous.TotalMemory - first.TotalMemory,
        };

        public static MeasureSnapshot Next(MeasureSnapshot initial, MeasureSnapshot previous) =>
            Diff(new MeasureSnapshot(), initial, previous);

        public static MeasureSnapshot Zero { get; } = new()
        {
            Elapsed = 0,
            MessageCount = 0,
            TotalMemory = ByteSize.Zero,
            TotalAllocatedBytes = ByteSize.Zero,
            AllocatedThreadMemory = ByteSize.Zero,
            PauseTime = TimeSpan.Zero,
            GcCount0 = 0,
            GcCount1 = 0,
            GcCount2 = 0,
            DeltaAllocatedBytes = ByteSize.Zero,
            DeltaTotalMemory = ByteSize.Zero,
        };

        public readonly override string ToString() =>
            $"""
               TimeStamp: {new DateTime(Timestamp, DateTimeKind.Utc):h:mm:ss.ffffff}
               Msg Count: {MessageCount:N0}
               Duration: {TimeSpan.FromTicks(Elapsed).TotalSeconds:F4}s
               Total Memory: {TotalMemory}
               Delta Memory: {DeltaTotalMemory}
               Total Alloc: {TotalAllocatedBytes}
               Delta Alloc: {DeltaAllocatedBytes}
               Thread Alloc: {AllocatedThreadMemory} ({ThreadId})
               GC Pause: {PauseTime.TotalMilliseconds:F}ms
               GC Collection: G1({GcCount0}) / G2({GcCount1}) / G3({GcCount2})
             """;
    }
}