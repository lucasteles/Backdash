using Backdash.Core;
using Backdash.Network.Client;
using Backdash.Serialization;

namespace Backdash.Benchmarks.Network;

static class Factory
{
    public static PeerClient<PingMessage> CreateUdpClient(
        int port,
        out PeerObserverGroup<PingMessage> observers
    )
    {
        observers = new();
        PeerClient<PingMessage> client = new(
            new UdpSocket(port),
            BinarySerializerFactory.ForEnum<PingMessage>(),
            observers,
            Logger.CreateConsoleLogger(LogLevel.None)
        );

        return client;
    }
}
