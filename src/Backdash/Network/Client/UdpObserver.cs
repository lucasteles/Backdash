using System.Net;
namespace Backdash.Network.Client;
interface IUdpObserver<in T> where T : struct
{
    ValueTask OnUdpMessage(T message, SocketAddress from, int bytesReceived, CancellationToken stoppingToken);
}
sealed class UdpObserverGroup<T> : IUdpObserver<T>
    where T : struct
{
    readonly List<IUdpObserver<T>> observers = [];
    public void Add(IUdpObserver<T> observer) => observers.Add(observer);
    public void Remove(IUdpObserver<T> observer) => observers.Remove(observer);
    public async ValueTask OnUdpMessage(
        T message, SocketAddress from, int bytesReceived, CancellationToken stoppingToken
    )
    {
        for (var i = 0; i < observers.Count; i++)
            await observers[i].OnUdpMessage(message, from, bytesReceived, stoppingToken).ConfigureAwait(false);
    }
}
