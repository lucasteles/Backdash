using System.Net;
using Backdash.Network.Client;

namespace Backdash.Benchmarks.Ping;

sealed class PingMessageHandler(IPeerClient<PingMessage> sender) : IPeerObserver<PingMessage>
{
    public static long TotalProcessed => processedCount;
    static long processedCount;

    public async ValueTask OnPeerMessage(
        PingMessage message,
        SocketAddress from,
        int bytesReceived,
        CancellationToken stoppingToken
    )
    {
        if (stoppingToken.IsCancellationRequested)
            return;
        Interlocked.Increment(ref processedCount);
        var reply = message switch
        {
            PingMessage.Ping => PingMessage.Pong,
            PingMessage.Pong => PingMessage.Ping,
            _ => throw new ArgumentOutOfRangeException(nameof(message), message, null),
        };
        try
        {
            await sender.SendTo(from, reply, null, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // skip
        }
    }
}
