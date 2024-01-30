using nGGPO.Serialization;

namespace nGGPO.Tests.Utils.Fixtures;

[Serializable]
sealed class Peer2PeerFixture<T> : IDisposable where T : struct
{
    public readonly UdpClientContext<T> Server;
    public readonly UdpClientContext<T> Client;

    Task tasks = Task.CompletedTask;

    readonly CancellationTokenSource cts = new();

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

        if (start) Start(cts.Token);
    }

    public void Start(CancellationToken ct) => tasks = Task.WhenAll(Server.Socket.Start(ct), Client.Socket.Start(ct));

    public void Deconstruct(out UdpClientContext<T> client, out UdpClientContext<T> server) =>
        (client, server) = (Client, Server);

    public void Dispose()
    {
        Client.Dispose();
        Server.Dispose();
        tasks.WaitAsync(TimeSpan.FromSeconds(2)).GetAwaiter().GetResult();
    }
}
