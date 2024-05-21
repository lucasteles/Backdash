using System.Net;
using Backdash.Benchmarks.Ping;
using Backdash.Core;
using Backdash.Network.Client;
using Backdash.Serialization;

var totalDuration = TimeSpan.FromSeconds(10);
var snapshotInterval = TimeSpan.FromSeconds(0);
var printSnapshots = false;

var logger = Logger.CreateConsoleLogger(LogLevel.None);
using BackgroundJobManager jobs = new(logger);

const int bufferSize = Max.CompressedBytes * Max.NumberOfPlayers;
var sendBuffer1 = Mem.CreatePinnedMemory(bufferSize);
var sendBuffer2 = Mem.CreatePinnedMemory(bufferSize);

using var peer1 = CreateClient(9000, sendBuffer1);
using var peer2 = CreateClient(9001, sendBuffer2);

using CancellationTokenSource cts = new();
cts.CancelAfter(totalDuration);
var stopToken = cts.Token;

Console.WriteLine("Running.");

var tasks = jobs.Start(stopToken);

await using Measurer measurer = new(snapshotInterval);
measurer.Start();

IPEndPoint peer2Endpoint = new(IPAddress.Loopback, 9001);
_ = peer1.SendTo(peer2Endpoint.Serialize(), PingMessage.Ping, sendBuffer1).AsTask();

Console.WriteLine("Press enter to stop.");
SpinWait.SpinUntil(() => Console.KeyAvailable || stopToken.IsCancellationRequested);

await cts.CancelAsync();

await tasks.ConfigureAwait(false);

measurer.Stop();

Console.Clear();
Console.WriteLine(measurer.Summary(printSnapshots));

IPeerClient<PingMessage> CreateClient(int port, Memory<byte>? buffer = null)
{
    PeerObserverGroup<PingMessage> observers = new();
    PeerClient<PingMessage> peer = new(
        new UdpSocket(port),
        BinarySerializerFactory.ForEnum<PingMessage>(),
        observers,
        logger,
        bufferSize
    );
    observers.Add(new PingMessageHandler(peer, buffer));
    jobs.Register(peer);
    return peer;
}
