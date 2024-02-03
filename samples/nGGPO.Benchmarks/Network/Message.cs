using System.Net;
using nGGPO.Network.Client;

#pragma warning disable CS9113 // Parameter is unread.

namespace nGGPO.Benchmarks.Network;

public enum PingMessage : long
{
    Ping = 111111111,
    Pong = 999999999,
}

sealed class PingMessageHandler(
    string name,
    byte[]? sendBuffer = null,
    long spinCount = 1
) : IUdpObserver<PingMessage>
{
    long processed;
    long currentSpins;

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
            case PingMessage.Ping:
                if (sendBuffer is null)
                    await sender.SendTo(from, PingMessage.Pong, stoppingToken);
                else
                    await sender.SendTo(from, PingMessage.Pong, sendBuffer, stoppingToken);
                break;
            case PingMessage.Pong:
                if (currentSpins >= spinCount)
                {
                    Interlocked.Exchange(ref currentSpins, 0);
                    Interlocked.Increment(ref processed);
                    OnProcessed.Invoke(processed);
                    break;
                }

                Interlocked.Increment(ref currentSpins);
                if (sendBuffer is null)
                    await sender.SendTo(from, PingMessage.Ping, stoppingToken);
                else
                    await sender.SendTo(from, PingMessage.Ping, sendBuffer, stoppingToken);

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(message), message, null);
        }

#if DEBUG
        Console.WriteLine(
            $"{DateTime.Now:T} - {name} [{processed}|{currentSpins}]: {message} from {from}");
#endif
    }
}