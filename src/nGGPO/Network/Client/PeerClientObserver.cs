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

sealed class PeerClientEventObserver<T> : IPeerClientObserver<T>
    where T : struct
{
    public event Func<UdpPeerClient<T>, T, SocketAddress, CancellationToken, ValueTask> OnMessage = delegate
    {
        return ValueTask.CompletedTask;
    };

    ValueTask IPeerClientObserver<T>.OnMessage(
        UdpPeerClient<T> sender, T message, SocketAddress from, CancellationToken stoppingToken
    ) =>
        OnMessage(sender, message, from, stoppingToken);
}

sealed class PeerClientObserver<T>(
    Func<UdpPeerClient<T>, T, SocketAddress, CancellationToken, ValueTask> onMessage)
    : IPeerClientObserver<T>
    where T : struct
{
    public ValueTask OnMessage(
        UdpPeerClient<T> sender,
        T message,
        SocketAddress from,
        CancellationToken stoppingToken
    ) =>
        onMessage(sender, message, from, stoppingToken);
}
