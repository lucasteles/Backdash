using nGGPO;
using nGGPO.Lifecycle;
using nGGPO.Network.Client;
using nGGPO.PingTest;
using nGGPO.Serialization;

Measurer measurer = new();
ConsoleLogger logger = new() {EnabledLevel = LogLevel.Off};
await using BackgroundJobManager jobs = new(logger);

using var peer1 = CreateClient(9000, measurer);
using var peer2 = CreateClient(9001);

using CancellationTokenSource cts = new();
cts.CancelAfter(TimeSpan.FromSeconds(10));
var stopToken = cts.Token;
Console.WriteLine("Running.");
var tasks = jobs.Start(stopToken);

measurer.Start();
_ = peer1.SendTo(peer2.Address, Message.Ping);

Console.WriteLine("Press enter to stop.");
SpinWait.SpinUntil(() => Console.KeyAvailable || stopToken.IsCancellationRequested);
cts.Cancel();
measurer.Snapshot();
await tasks.ConfigureAwait(false);
measurer.Stop();

var totalSent = peer1.TotalBytesSent + peer2.TotalBytesSent;
Console.Clear();
Console.WriteLine(measurer.Summary(totalSent));

IUdpClient<Message> CreateClient(int port, Measurer? m = null)
{
    UdpObservableClient<Message> udp = new(port,
        BinarySerializerFactory.ForEnum<Message>(), logger);

    udp.Observers.Add(new PingMessageHandler(m));

    udp.EnableLogs(false);
    jobs.Register(udp);
    return udp.Client;
}