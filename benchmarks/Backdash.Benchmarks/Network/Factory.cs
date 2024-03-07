using Backdash.Core;
using Backdash.Network.Client;
using Backdash.Serialization;
namespace Backdash.Benchmarks.Network;
static class Factory
{
    public static UdpClient<PingMessage> CreateUdpClient(
        int port,
        out UdpObserverGroup<PingMessage> observers
    )
    {
        observers = new();
        UdpClient<PingMessage> client = new(
            new UdpSocket(port),
            BinarySerializerFactory.ForEnum<PingMessage>(),
            observers,
            Logger.CreateConsoleLogger(LogLevel.Off)
        );
        return client;
    }
}