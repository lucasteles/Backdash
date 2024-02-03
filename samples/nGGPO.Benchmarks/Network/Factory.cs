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
        UdpObserverGroup<PingMessage> observers = new();

        UdpClient<PingMessage> udp = new(
            new UdpSocket(port),
            BinarySerializerFactory.ForEnum<PingMessage>(),
            observers,
            new ConsoleLogger {EnabledLevel = LogLevel.Off}
        );

        observers.Add(observer);

        return udp;
    }
}