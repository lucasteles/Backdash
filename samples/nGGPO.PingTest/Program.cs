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

using CancellationTokenSource source = new();

Console.WriteLine("Started.");
source.CancelAfter(TimeSpan.FromSeconds(10));

var tasks = jobs.Start(source.Token);
measurer.Start();
await peer1.SendTo(peer2.Address, Message.Ping);
await tasks;
measurer.Stop();

var totalSent = peer1.TotalBytesSent + peer2.TotalBytesSent;
Console.WriteLine(measurer.Summary(totalSent));
Console.WriteLine("Finished.");

IUdpClient<Message> CreateClient(int port, Measurer? m = null)
{
    UdpObservableClient<Message> udp = new(port,
        BinarySerializerFactory.ForEnum<Message>(), logger);

    udp.Observers.Add(new PingMessageHandler(m));

    udp.EnableLogs(false);
    jobs.Register(udp);
    return udp.Client;
}