using Backdash.Core;
using Backdash.Network.Client;
using Backdash.Serialization;

namespace Backdash.Benchmarks.Network;

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
            Logger.CreateConsoleLogger(LogLevel.Off)
        );

        observers.Add(observer);

        return udp;
    }
}