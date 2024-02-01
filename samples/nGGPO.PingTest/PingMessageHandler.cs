using System.Net;
using nGGPO.Network.Client;

sealed class PingMessageHandler : IUdpObserver<PingMessage>
{
    public static long TotalProcessed => processedCount;

    static long processedCount;

    public ValueTask OnUdpMessage(
        IUdpClient<PingMessage> sender,
        PingMessage message,
        SocketAddress from,
        CancellationToken stoppingToken
    )
    {
        if (stoppingToken.IsCancellationRequested)
            return ValueTask.CompletedTask;

        Interlocked.Increment(ref processedCount);

        return message switch
        {
            PingMessage.Ping => sender.SendTo(from, PingMessage.Pong, stoppingToken),
            PingMessage.Pong => sender.SendTo(from, PingMessage.Ping, stoppingToken),
            _ => throw new ArgumentOutOfRangeException(nameof(message), message, null),
        };
    }
}