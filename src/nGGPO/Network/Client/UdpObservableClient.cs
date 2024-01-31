using nGGPO.Serialization;

namespace nGGPO.Network.Client;

interface IUdpObservableClient<T> : IDisposable where T : struct
{
    UdpObserverGroup<T> Observers { get; }
    IUdpClient<T> Client { get; }
}

sealed class UdpObservableClient<T> : IUdpObservableClient<T> where T : struct
{
    public UdpObserverGroup<T> Observers { get; }
    public IUdpClient<T> Client { get; }

    public UdpObservableClient(int port, IBinarySerializer<T> serializer, ILogger logger)
    {
        Observers = new();
        Client = new UdpClient<T>(port, Observers, serializer, logger);
    }

    public void Dispose() => Client.Dispose();
}
