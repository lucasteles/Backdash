using System.Diagnostics;
using System.Net;
using nGGPO.Network.Client;
using nGGPO.Serialization;
using UdpPeerClient = nGGPO.Network.Client.UdpPeerClient<Message>;

var processedMessageCount = 0UL;

ValueTask ProcessMessage(UdpPeerClient client, Message message, SocketAddress sender,
    CancellationToken ct)
{
    if (ct.IsCancellationRequested) return ValueTask.CompletedTask;
    Interlocked.Increment(ref processedMessageCount);
    return message switch
    {
        Message.Ping => client.SendTo(sender, Message.Pong, ct),
        Message.Pong => client.SendTo(sender, Message.Ping, ct),
        _ => throw new ArgumentOutOfRangeException(nameof(message), message, null)
    };
}

UdpPeerClient peer1 = new(9000,
    new PeerClientObserver<Message>(ProcessMessage),
    BinarySerializerFactory.ForEnum<Message>())
{
    LogsEnabled = false,
};

UdpPeerClient peer2 = new(9001,
    new PeerClientObserver<Message>(ProcessMessage),
    BinarySerializerFactory.ForEnum<Message>())
{
    LogsEnabled = false,
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
double totalMemory = GC.GetTotalMemory(true);
watch.Start();

await peer1.SendTo(peer2.Address, Message.Ping);
await tasks;

watch.Stop();
totalMemory = (GC.GetTotalMemory(true) - totalMemory) / 1024.0;
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
    processedMessageCount, watch.Elapsed,
    totalMemory, totalMemory / 1024.0,
    pauseTime, gcCount[0], gcCount[1], gcCount[2],
    sizeof(Message), totalSent, totalSent / 1024.0
);


public enum Message
{
    Ping = 2,
    Pong = 4,
}

sealed class PeerClientObserver<T>(
    Func<UdpPeerClient<T>, T, SocketAddress, CancellationToken, ValueTask> onMessage)
    : IPeerClientObserver<T>
    where T : struct
{
    public ValueTask OnMessage(
        UdpPeerClient<T> sender,
        T message,
        SocketAddress from,
        CancellationToken stoppingToken
    ) =>
        onMessage(sender, message, from, stoppingToken);
}