using System.Diagnostics;

namespace nGGPO.PingTest;

public sealed class Measurer
{
    readonly Stopwatch watch = new();
    TimeSpan pauseTime;
    double totalMemory;
    int gcCount0, gcCount1, gcCount2;

    public void Start()
    {
        (gcCount0, gcCount1, gcCount2) =
            (GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2));

        pauseTime = GC.GetTotalPauseDuration();
        totalMemory = GC.GetTotalMemory(true);

        watch.Start();
    }

    public void Stop()
    {
        watch.Stop();
        totalMemory = (GC.GetTotalMemory(true) - totalMemory) / 1024.0;
        gcCount0 = GC.CollectionCount(0) - gcCount0;
        gcCount1 = GC.CollectionCount(1) - gcCount1;
        gcCount2 = GC.CollectionCount(2) - gcCount2;
        pauseTime = GC.GetTotalPauseDuration() - pauseTime;
    }

    public string Summary(double totalSent) =>
        $"""
         --- Summary ---
         Msg Count: {PingMessageHandler.TotalProcessed}
         Total Sent: {totalSent:F}KB | {totalSent / 1024:F}MB
         Msg Size: {sizeof(Message)} Bytes

         Time: {watch.Elapsed:g}
         Total Alloc: {totalMemory:F}KB | {totalMemory / 1024.0:F}MB
         GC Pause: {pauseTime:g}
         Collect Count: G1({gcCount0}); G2({gcCount1}); G3({gcCount2})
         ---------------
         """;
}