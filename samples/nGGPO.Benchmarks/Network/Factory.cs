using nGGPO.Core;
using nGGPO.Network.Client;
using nGGPO.Serialization;

namespace nGGPO.Benchmarks.Network;

static class Factory
{
    public static UdpClient<PingMessage> CreatePingClient(
        IUdpObserver<PingMessage> observer,
        int port
    )
    {
        UdpObservableClient<PingMessage> udp = new(
            port,
            BinarySerializerFactory.ForEnum<PingMessage>(),
            new ConsoleLogger {EnabledLevel = LogLevel.Off}
        );

        udp.Observers.Add(observer);
        udp.EnableLogs(false);

        return (UdpClient<PingMessage>) udp.Client;
    }
}