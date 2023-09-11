using Backdash.Core;
using Backdash.Network.Client;
using Backdash.PingTest;
using Backdash.Serialization;

var totalDuration = TimeSpan.FromSeconds(10);
var snapshotInterval = TimeSpan.FromSeconds(0);
var printSnapshots = false;

ConsoleLogger logger = new() {EnabledLevel = LogLevel.Off};
await using BackgroundJobManager jobs = new(logger);
const int bufferSize = Max.CompressedBytes * Max.MsgPlayers;
var sendBuffer1 = Mem.CreatePinnedBuffer(bufferSize);
var sendBuffer2 = Mem.CreatePinnedBuffer(bufferSize);

using var peer1 = CreateClient(9000, sendBuffer1);
using var peer2 = CreateClient(9001, sendBuffer2);

using CancellationTokenSource cts = new();
cts.CancelAfter(totalDuration);
var stopToken = cts.Token;
Console.WriteLine("Running.");
var tasks = jobs.Start(stopToken);

await using Measurer measurer = new(snapshotInterval);
measurer.Start();
_ = peer1.SendTo(peer2.Address, PingMessage.Ping, sendBuffer1).AsTask();

Console.WriteLine("Press enter to stop.");
SpinWait.SpinUntil(() => Console.KeyAvailable || stopToken.IsCancellationRequested);
cts.Cancel();
await tasks.ConfigureAwait(false);
measurer.Stop();
Console.Clear();
Console.WriteLine(measurer.Summary(printSnapshots));

IUdpClient<PingMessage> CreateClient(int port, Memory<byte>? buffer = null)
{
    UdpObserverGroup<PingMessage> observers = new();

    UdpClient<PingMessage> udp = new(
        new UdpSocket(port),
        BinarySerializerFactory.ForEnum<PingMessage>(),
        observers,
        logger,
        bufferSize
    );

    observers.Add(new PingMessageHandler(buffer));
    jobs.Register(udp);

    return udp;
}