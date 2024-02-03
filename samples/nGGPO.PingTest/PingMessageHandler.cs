using System.Net;
using nGGPO.Network.Client;

namespace nGGPO.PingTest;

sealed class PingMessageHandler : IUdpObserver<PingMessage>
{
    public static long TotalProcessed => processedCount;

    static long processedCount;

    public async ValueTask OnUdpMessage(
        IUdpClient<PingMessage> sender,
        PingMessage message,
        SocketAddress from,
        CancellationToken stoppingToken
    )
    {
        if (stoppingToken.IsCancellationRequested)
            return;

        Interlocked.Increment(ref processedCount);

        switch (message)
        {
            case PingMessage.Ping:
                await sender.SendTo(from, PingMessage.Pong, stoppingToken);
                break;
            case PingMessage.Pong:
                await sender.SendTo(from, PingMessage.Ping, stoppingToken);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(message), message, null);
        }
    }
}