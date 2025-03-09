using System.Net;
using Backdash.Network.Client;

namespace Backdash.Tests.TestUtils.Network;

sealed class PeerEventObserver<T> : IPeerObserver<T>
    where T : struct
{
    public event Action<T, SocketAddress, int> OnMessage = delegate { };

    void IPeerObserver<T>.OnPeerMessage(ref readonly T message, in SocketAddress from, int bytesReceived) =>
        OnMessage(message, from, bytesReceived);
}
