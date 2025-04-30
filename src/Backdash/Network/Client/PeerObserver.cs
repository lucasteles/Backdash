using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Backdash.Network.Client;

/// <summary>
///     Observe a <see cref="PeerClient{T}" />
/// </summary>
/// <typeparam name="T"></typeparam>
interface IPeerObserver<T>
{
    /// <summary>
    ///     Handle new message from peer
    /// </summary>
    void OnPeerMessage(ref readonly T message, in SocketAddress from, int bytesReceived);
}

sealed class PeerObserverGroup<T> : IPeerObserver<T>
    where T : struct
{
    readonly List<IPeerObserver<T>> observers = [];
    public void Add(IPeerObserver<T> observer) => observers.Add(observer);
    public void Remove(IPeerObserver<T> observer) => observers.Remove(observer);

    public void OnPeerMessage(ref readonly T message, in SocketAddress from, int bytesReceived)
    {
        var span = CollectionsMarshal.AsSpan(observers);
        ref var pointer = ref MemoryMarshal.GetReference(span);
        ref var end = ref Unsafe.Add(ref pointer, span.Length);

        while (Unsafe.IsAddressLessThan(ref pointer, ref end))
        {
            pointer.OnPeerMessage(in message, in from, bytesReceived);
            pointer = ref Unsafe.Add(ref pointer, 1)!;
        }
    }
}
