using System.Net;

namespace nGGPO.Network.Client;

interface IPeerClientObserver<T> where T : struct
{
    ValueTask OnMessage(
        UdpPeerClient<T> sender,
        T message,
        SocketAddress from,
        CancellationToken stoppingToken
    );
}

sealed class PeerClientObserverGroup<T> : IPeerClientObserver<T>
    where T : struct
{
    readonly List<IPeerClientObserver<T>> observers = [];

    public void Add(IPeerClientObserver<T> observer) => observers.Add(observer);
    public void Remove(IPeerClientObserver<T> observer) => observers.Remove(observer);

    public async ValueTask OnMessage(UdpPeerClient<T> sender, T message, SocketAddress from,
        CancellationToken stoppingToken)
    {
        for (var i = 0; i < observers.Count; i++)
            await observers[i].OnMessage(sender, message, from, stoppingToken).ConfigureAwait(false);
    }
}
