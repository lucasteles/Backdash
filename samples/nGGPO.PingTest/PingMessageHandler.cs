using System.Net;
using nGGPO.Network.Client;
using nGGPO.PingTest;

sealed class PingMessageHandler(Measurer? measurer = null) : IUdpObserver<Message>
{
    public static long TotalProcessed => processedCount;

    static long processedCount;

    public ValueTask OnUdpMessage(IUdpClient<Message> sender, Message message, SocketAddress from,
        CancellationToken stoppingToken)
    {
        if (stoppingToken.IsCancellationRequested) return ValueTask.CompletedTask;
        Interlocked.Increment(ref processedCount);

        if (measurer is not null && processedCount % Measurer.Factor == 0)
            measurer.Snapshot();

        return message switch
        {
            Message.Ping => sender.SendTo(from, Message.Pong, stoppingToken),
            Message.Pong => sender.SendTo(from, Message.Ping, stoppingToken),
            _ => throw new ArgumentOutOfRangeException(nameof(message), message, null),
        };
    }
}