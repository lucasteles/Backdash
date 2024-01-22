using nGGPO.Serialization;

namespace nGGPO.Tests;

[Serializable]
public sealed class Peer2PeerFixture<T> : IDisposable where T : struct
{
    public readonly UdpClientContext<T> Server;
    public readonly UdpClientContext<T> Client;

    Task tasks = Task.CompletedTask;

    public Peer2PeerFixture(
        IBinarySerializer<T> serializer,
        int? serverPort = null,
        int? clientPort = null,
        bool start = true
    )
    {
        Server = new(serializer, serverPort);
        Client = new(serializer, clientPort);

        Server.Socket.LogsEnabled = false;
        Client.Socket.LogsEnabled = false;

        if (start) Start();
    }

    public void Start() => tasks = Task.WhenAll(Server.Socket.StartPumping(), Client.Socket.StartPumping());

    public void Deconstruct(out UdpClientContext<T> client, out UdpClientContext<T> server) =>
        (client, server) = (Client, Server);

    public void Dispose()
    {
        Client.Dispose();
        Server.Dispose();
        tasks.WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
    }
}
