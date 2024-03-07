using System.Net;
using Backdash.Network.Client;
namespace Backdash.Tests.Utils.Network;
sealed class UdpEventObserver<T> : IUdpObserver<T>
    where T : struct
{
    public event Func<T, SocketAddress, int, CancellationToken, ValueTask> OnMessage = delegate
    {
        return ValueTask.CompletedTask;
    };
    ValueTask IUdpObserver<T>.OnUdpMessage(
        T message, SocketAddress from, int bytesReceived, CancellationToken stoppingToken
    ) => OnMessage(message, from, bytesReceived, stoppingToken);
}
