using System.Net;
using nGGPO.Network.Client;

namespace nGGPO.Tests.Utils.Network;

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
