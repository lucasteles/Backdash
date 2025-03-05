using System.Net;
using Backdash.Core;
using Backdash.Network.Client;

#pragma warning disable CS9113 // Parameter is unread.
namespace Backdash.Benchmarks.Network;

public enum PingMessage : long
{
    Ping = 111111111,
    Pong = 999999999,
}

sealed class PingMessageHandler(string name, IPeerClient<PingMessage> sender) : IPeerObserver<PingMessage>
{
    long processedCount;
    long badMessages;
    public long ProcessedCount => processedCount;
    public long BadMessages => badMessages;
    public event Action<long> OnProcessed = delegate { };

    public void OnPeerMessage(ref readonly PingMessage message, in SocketAddress from, int bytesReceived
    )
    {
        Interlocked.Increment(ref processedCount);

        if (!Enum.IsDefined(message))
            Interlocked.Increment(ref badMessages);

        var reply = message switch
        {
            PingMessage.Ping => PingMessage.Pong,
            PingMessage.Pong => PingMessage.Ping,
            _ => throw new ArgumentOutOfRangeException(nameof(message), message, null),
        };

        ThrowIf.Assert(sender.TrySendTo(from, reply));
        OnProcessed(processedCount);
#if DEBUG
        Console.WriteLine(
            $"{DateTime.Now:T} - {name} [{processedCount}]: {message} from {from}");
#endif
    }
}
