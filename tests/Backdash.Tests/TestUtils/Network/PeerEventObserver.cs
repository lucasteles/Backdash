using System.Net;
using Backdash.Network.Client;

namespace Backdash.Tests.TestUtils.Network;

sealed class PeerEventObserver<T> : IPeerObserver<T>
    where T : struct
{
    public event Func<T, SocketAddress, int, CancellationToken, ValueTask> OnMessage = delegate
    {
        return ValueTask.CompletedTask;
    };
    ValueTask IPeerObserver<T>.OnPeerMessage(
        T message, SocketAddress from, int bytesReceived, CancellationToken stoppingToken
    ) => OnMessage(message, from, bytesReceived, stoppingToken);
}
