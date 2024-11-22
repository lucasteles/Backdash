using System.Diagnostics;
using System.Net;
using Backdash.Network.Client;

namespace Backdash.Benchmarks.Ping;

sealed class PingMessageHandler(IPeerClient<PingMessage> sender) : IPeerObserver<PingMessage>
{
    public static long TotalProcessed => processedCount;
    static long processedCount;

    public void OnPeerMessage(
        in PingMessage message,
        SocketAddress from,
        int bytesReceived
    )
    {
        Interlocked.Increment(ref processedCount);

        var reply = message switch
        {
            PingMessage.Ping => PingMessage.Pong,
            PingMessage.Pong => PingMessage.Ping,
            _ => throw new ArgumentOutOfRangeException(nameof(message), message, null),
        };

        Trace.Assert(sender.TrySendTo(from, reply));
    }
}
