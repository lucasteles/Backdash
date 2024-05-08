using System.Net;
using System.Runtime.CompilerServices;

namespace Backdash.Network.Client;

/// <summary>
/// Observe a <see cref="IPeerClient{T}"/>
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IPeerObserver<in T> where T : struct
{
    /// <summary>
    /// Handle new message from peer
    /// </summary>
    ValueTask OnPeerMessage(T message, SocketAddress from, int bytesReceived, CancellationToken stoppingToken);
}

sealed class PeerObserverGroup<T> : IPeerObserver<T>
    where T : struct
{
    readonly List<IPeerObserver<T>> observers = [];
    public void Add(IPeerObserver<T> observer) => observers.Add(observer);
    public void Remove(IPeerObserver<T> observer) => observers.Remove(observer);

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
    public async ValueTask OnPeerMessage(
        T message, SocketAddress from, int bytesReceived, CancellationToken stoppingToken
    )
    {
        for (var i = 0; i < observers.Count; i++)
            await observers[i].OnPeerMessage(message, from, bytesReceived, stoppingToken).ConfigureAwait(false);
    }
}
