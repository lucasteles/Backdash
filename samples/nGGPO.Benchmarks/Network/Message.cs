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
    Memory<byte> sendBuffer
) : IUdpObserver<PingMessage>
{
    long processedCount;
    long badMessages;

    public long ProcessedCount => processedCount;
    public long BadMessages => badMessages;

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

        if (stoppingToken.IsCancellationRequested)
            return;

        Interlocked.Increment(ref processedCount);

        if (!Enum.IsDefined(message))
            Interlocked.Increment(ref badMessages);

        var reply = message switch
        {
            PingMessage.Ping => PingMessage.Pong,
            PingMessage.Pong => PingMessage.Ping,
            _ => throw new ArgumentOutOfRangeException(nameof(message), message, null),
        };

        try
        {
            if (sendBuffer.IsEmpty)
                await sender.SendTo(from, reply, stoppingToken);
            else
                await sender.SendTo(from, reply, sendBuffer, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // skip
        }

        OnProcessed(processedCount);

#if DEBUG
        Console.WriteLine(
            $"{DateTime.Now:T} - {name} [{processedCount}]: {message} from {from}");
#endif
    }
}