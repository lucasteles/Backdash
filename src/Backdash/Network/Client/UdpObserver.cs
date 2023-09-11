using System.Net;

namespace Backdash.Network.Client;

interface IUdpObserver<T> where T : struct
{
    ValueTask OnUdpMessage(
        IUdpClient<T> sender,
        T message,
        SocketAddress from,
        CancellationToken stoppingToken
    );
}

sealed class UdpObserverGroup<T> : IUdpObserver<T>
    where T : struct
{
    readonly List<IUdpObserver<T>> observers = [];

    public void Add(IUdpObserver<T> observer) => observers.Add(observer);
    public void Remove(IUdpObserver<T> observer) => observers.Remove(observer);

    public async ValueTask OnUdpMessage(IUdpClient<T> sender, T message, SocketAddress from,
        CancellationToken stoppingToken)
    {
        for (var i = 0; i < observers.Count; i++)
            await observers[i].OnUdpMessage(sender, message, from, stoppingToken).ConfigureAwait(false);
    }
}
