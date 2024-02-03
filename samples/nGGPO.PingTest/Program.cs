using nGGPO;
using nGGPO.Core;
using nGGPO.Network.Client;
using nGGPO.PingTest;
using nGGPO.Serialization;

var totalDuration = TimeSpan.FromSeconds(10);
var snapshotInterval = TimeSpan.FromSeconds(1);
var printSnapshots = false;

ConsoleLogger logger = new() {EnabledLevel = LogLevel.Off};
await using BackgroundJobManager jobs = new(logger);

using var peer1 = CreateClient(9000);
using var peer2 = CreateClient(9001);

using CancellationTokenSource cts = new();
cts.CancelAfter(totalDuration);
var stopToken = cts.Token;
Console.WriteLine("Running.");
var tasks = jobs.Start(stopToken);

await using Measurer measurer = new(snapshotInterval);
measurer.Start();
_ = peer1.SendTo(peer2.Address, PingMessage.Ping).AsTask();

Console.WriteLine("Press enter to stop.");
SpinWait.SpinUntil(() => Console.KeyAvailable || stopToken.IsCancellationRequested);
cts.Cancel();
await tasks.ConfigureAwait(false);
measurer.Stop();
var totalSent = peer1.TotalBytesSent + peer2.TotalBytesSent;
Console.Clear();
Console.WriteLine(measurer.Summary(totalSent, printSnapshots));


IUdpClient<PingMessage> CreateClient(int port)
{
    UdpObservableClient<PingMessage> udp = new(port,
        BinarySerializerFactory.ForEnum<PingMessage>(), logger);

    udp.Observers.Add(new PingMessageHandler());

    udp.EnableLogs(false);
    jobs.Register(udp);
    return udp.Client;
}