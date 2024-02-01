using System.Net;
using nGGPO.Network.Client;

#pragma warning disable CS9113 // Parameter is unread.

namespace nGGPO.Benchmarks.Network;

public enum PingMessage
{
    HandShake = 2,
    Ping = 4,
    Pong = 8,
}

sealed class PingMessageHandler(string name) : IUdpObserver<PingMessage>
{
    long pendingResponses;
    long processed;

    public long PendingCount => pendingResponses;
    public long ProcessedCount => processed;

    public event Action<long> OnProcessed = delegate { };

    public async ValueTask OnUdpMessage(
        IUdpClient<PingMessage> sender,
        PingMessage message,
        SocketAddress from,
        CancellationToken stoppingToken
    )
    {
        if (stoppingToken.IsCancellationRequested)
            return;

        switch (message)
        {
            case PingMessage.HandShake:
                Interlocked.Increment(ref pendingResponses);
                await sender.SendTo(from, PingMessage.Ping, stoppingToken);
                break;
            case PingMessage.Ping:
                await sender.SendTo(from, PingMessage.Pong, stoppingToken);
                break;
            case PingMessage.Pong:
                Interlocked.Decrement(ref pendingResponses);
                Interlocked.Increment(ref processed);
                OnProcessed.Invoke(processed);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(message), message, null);
        }

#if DEBUG
        Console.WriteLine($"{name} [{pendingResponses}|{processed}]: {message} from {from}");
#endif
    }
}