using nGGPO.Core;
using nGGPO.Serialization;

namespace nGGPO.Network.Client;

interface IUdpObservableClient<T> : IBackgroundJob, IDisposable where T : struct
{
    UdpObserverGroup<T> Observers { get; }
    IUdpClient<T> Client { get; }
}

sealed class UdpObservableClient<T> : IUdpObservableClient<T> where T : struct
{
    readonly UdpClient<T> client;

    public UdpObserverGroup<T> Observers { get; }
    public IUdpClient<T> Client => client;

    public UdpObservableClient(int port, IBinarySerializer<T> serializer, ILogger logger)
    {
        Observers = new();

        client = new UdpClient<T>(
            new UdpSocket(port),
            Observers,
            serializer,
            logger
        );
    }

    public string JobName => client.JobName;
    public Task Start(CancellationToken ct) => client.Start(ct);

    public void EnableLogs(bool enabled) => client.LogsEnabled = enabled;

    public void Dispose() => client.Dispose();
}
