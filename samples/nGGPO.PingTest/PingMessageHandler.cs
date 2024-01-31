using System.Net;
using nGGPO.Network.Client;

sealed class PingMessageHandler : IUdpObserver<Message>
{
    public static ulong TotalProcessed => processedCount;

    static ulong processedCount;

    public ValueTask OnUdpMessage(IUdpClient<Message> sender, Message message, SocketAddress from,
        CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested) return ValueTask.CompletedTask;
        Interlocked.Increment(ref processedCount);
        return message switch
        {
            Message.Ping => sender.SendTo(from, Message.Pong, stoppingToken),
            Message.Pong => sender.SendTo(from, Message.Ping, stoppingToken),
            _ => throw new ArgumentOutOfRangeException(nameof(message), message, null),
        };
    }
}