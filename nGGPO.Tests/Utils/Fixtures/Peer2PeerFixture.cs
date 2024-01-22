using nGGPO.Serialization;

namespace nGGPO.Tests;

[Serializable]
public sealed class Peer2PeerFixture<T> : IDisposable where T : struct
{
    public readonly UdpClientContext<T> Server;
    public readonly UdpClientContext<T> Client;

    readonly Task tasks;

    public Peer2PeerFixture() : this(BinarySerializerFactory.Create<T>(), 9000, 9001)
    {
    }

    Peer2PeerFixture(
        IBinarySerializer<T> serializer,
        int serverPort,
        int clientPort
    )
    {
        Server = new(serializer, serverPort);
        Client = new(serializer, clientPort);

        Server.Socket.LogsEnabled = false;
        Client.Socket.LogsEnabled = false;

        tasks = Task.WhenAll(Server.Socket.Start(), Client.Socket.Start());
    }

    public void Deconstruct(out UdpClientContext<T> client, out UdpClientContext<T> server) =>
        (client, server) = (Client, Server);

    public void Dispose()
    {
        Client.Dispose();
        Server.Dispose();
        tasks.WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
    }
}
