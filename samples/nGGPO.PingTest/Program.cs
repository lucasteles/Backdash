using System.Diagnostics;
using nGGPO.PingTest;
using nGGPO.Serialization;
using UdpPeerClient = nGGPO.Network.UdpPeerClient<nGGPO.PingTest.Message>;

var msgCount = 0UL;

UdpPeerClient peer1 = new(9000, BinarySerializerFactory.ForEnum<Message>())
{
    LogsEnabled = false,
};

peer1.OnMessage += async (message, sender, token) =>
{
    if (token.IsCancellationRequested) return;
    Interlocked.Increment(ref msgCount);
    switch (message)
    {
        case Message.Ping:
            await peer1.SendTo(sender, Message.Pong, token);
            break;
        case Message.Pong:
            await peer1.SendTo(sender, Message.Ping, token);
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(message), message, null);
    }
};

UdpPeerClient peer2 = new(9001, BinarySerializerFactory.ForEnum<Message>())
{
    LogsEnabled = false,
};
peer2.OnMessage += async (message, sender, token) =>
{
    if (token.IsCancellationRequested) return;
    Interlocked.Increment(ref msgCount);
    switch (message)
    {
        case Message.Ping:
            await peer2.SendTo(sender, Message.Pong, token);
            break;
        case Message.Pong:
            await peer2.SendTo(sender, Message.Ping, token);
            break;
        default:
            throw new ArgumentOutOfRangeException(nameof(message), message, null);
    }
};

Stopwatch watch = new();
using CancellationTokenSource source = new();

Console.WriteLine("Started.");
var tasks = Task.WhenAll(peer1.StartPumping(source.Token), peer2.StartPumping(source.Token));

source.CancelAfter(TimeSpan.FromSeconds(10));
var gcCount = new[]
{
    GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2),
};
var pauseTime = GC.GetTotalPauseDuration();
double allocs = GC.GetTotalMemory(true);
watch.Start();

await peer1.SendTo(peer2.Address, Message.Ping);
await tasks;

watch.Stop();
allocs = (GC.GetTotalMemory(true) - allocs) / 1024.0;
gcCount[0] = GC.CollectionCount(0) - gcCount[0];
gcCount[1] = GC.CollectionCount(1) - gcCount[1];
gcCount[2] = GC.CollectionCount(2) - gcCount[2];
pauseTime = GC.GetTotalPauseDuration() - pauseTime;
var totalSent = (peer1.TotalBytesSent + peer2.TotalBytesSent) / 1024.0;
Console.WriteLine(
    """
    --- Summary ---
    Msg Count: {0}
    Time: {1:g}
    Msg Size: {8} Bytes
    Total Sent: {9:F}KB | {10:F}MB
    Total Alloc: {2:F}KB | {3:F}MB
    GC Pause: {4:g}
    Collect Count: G1({5}); G2({6}); G3({7})
    ---------------
    """,
    msgCount, watch.Elapsed,
    allocs, allocs / 1024.0,
    pauseTime, gcCount[0], gcCount[1], gcCount[2],
    sizeof(Message), totalSent, totalSent / 1024.0
);

#pragma warning disable S3903
namespace nGGPO.PingTest
{
    public enum Message
    {
        Ping = 2,
        Pong = 4,
    }
}