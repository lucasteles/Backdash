using System.Net;
using Backdash.Network.Client;
namespace Backdash.Benchmarks.Ping;
sealed class PingMessageHandler(
    IUdpClient<PingMessage> sender,
    Memory<byte>? buffer = null
) : IUdpObserver<PingMessage>
{
    public static long TotalProcessed => _processedCount;
    static long _processedCount;
    public async ValueTask OnUdpMessage(
        PingMessage message,
        SocketAddress from,
        int bytesReceived,
        CancellationToken stoppingToken
    )
    {
        if (stoppingToken.IsCancellationRequested)
            return;
        Interlocked.Increment(ref _processedCount);
        var reply = message switch
        {
            PingMessage.Ping => PingMessage.Pong,
            PingMessage.Pong => PingMessage.Ping,
            _ => throw new ArgumentOutOfRangeException(nameof(message), message, null),
        };
        try
        {
            if (buffer is null)
                await sender.SendTo(from, reply, stoppingToken);
            else
                await sender.SendTo(from, reply, buffer.Value, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // skip
        }
    }
}