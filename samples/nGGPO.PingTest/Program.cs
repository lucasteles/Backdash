using System.Diagnostics;
using System.Net;
using nGGPO.Network;
using nGGPO.Serialization;

var msgCount = 0UL;

UdpPeerClient<Message> peer1 = new(9000, BinarySerializerFactory.ForEnum<Message>())
{
    LogsEnabled = false,
};

peer1.OnMessage += async (message, sender, token) =>
{
    if (token.IsCancellationRequested)
        return;

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

UdpPeerClient<Message> peer2 = new(9001, BinarySerializerFactory.ForEnum<Message>())
{
    LogsEnabled = false,
};
peer2.OnMessage += async (message, sender, token) =>
{
    if (token.IsCancellationRequested)
        return;

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
var address2 = new IPEndPoint(IPAddress.Loopback, 9001).Serialize();

var gcCount = new[]
{
    GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2),
};
double allocs = GC.GetTotalMemory(true);
var pauseTime = GC.GetTotalPauseDuration();
watch.Start();

await peer1.SendTo(address2, Message.Ping, source.Token);
source.CancelAfter(TimeSpan.FromSeconds(10));

// await Console.In.ReadLineAsync(source.Token);
// if (!source.IsCancellationRequested)
//     await source.CancelAsync();

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
public enum Message : byte
{
    Ping = 2,
    Pong = 4,
}
