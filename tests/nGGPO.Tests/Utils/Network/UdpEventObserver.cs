using System.Net;
using nGGPO.Network.Client;

namespace nGGPO.Tests.Utils.Network;

sealed class UdpEventObserver<T> : IUdpObserver<T>
    where T : struct
{
    public event Func<IUdpClient<T>, T, SocketAddress, CancellationToken, ValueTask> OnMessage = delegate
    {
        return ValueTask.CompletedTask;
    };

    ValueTask IUdpObserver<T>.OnUdpMessage(IUdpClient<T> sender, T message, SocketAddress from,
        CancellationToken stoppingToken) =>
        OnMessage(sender, message, from, stoppingToken);
}
