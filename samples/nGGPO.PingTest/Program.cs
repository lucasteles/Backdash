using nGGPO;
using nGGPO.Lifecycle;
using nGGPO.Network.Client;
using nGGPO.PingTest;
using nGGPO.Serialization;

ConsoleLogger logger = new() {EnabledLevel = LogLevel.Off};
await using BackgroundJobManager jobs = new(logger);

using var peer1 = CreateClient(9000);
using var peer2 = CreateClient(9001);

using CancellationTokenSource source = new();
Measurer measurer = new();

Console.WriteLine("Started.");
source.CancelAfter(TimeSpan.FromSeconds(10));

var tasks = jobs.Start(source.Token);
measurer.Start();
await peer1.SendTo(peer2.Address, Message.Ping);
await tasks;
measurer.Stop();

var totalSent = (peer1.TotalBytesSent + peer2.TotalBytesSent) / 1024.0;
Console.WriteLine(measurer.Summary(totalSent));

IUdpClient<Message> CreateClient(int port)
{
    UdpObservableClient<Message> udp = new(port,
        BinarySerializerFactory.ForEnum<Message>(), logger);

    udp.Observers.Add(new PingMessageHandler());

    udp.EnableLogs(false);
    jobs.Register(udp);
    return udp.Client;
}