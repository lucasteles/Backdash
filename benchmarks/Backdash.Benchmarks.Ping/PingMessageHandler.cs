using System.Net;
using Backdash.Core;
using Backdash.Network.Client;

namespace Backdash.Benchmarks.Ping;

sealed class PingMessageHandler(PeerClient<PingMessage> sender) : IPeerObserver<PingMessage>
{
    public static long TotalProcessed => processedCount;
    static long processedCount;

    public void OnPeerMessage(ref readonly PingMessage message, in SocketAddress from, int bytesReceived)
    {
        Interlocked.Increment(ref processedCount);

        var reply = message switch
        {
            PingMessage.Ping => PingMessage.Pong,
            PingMessage.Pong => PingMessage.Ping,
            _ => throw new ArgumentOutOfRangeException(nameof(message), message, null),
        };

        ThrowIf.Assert(sender.TrySendTo(from, reply));
    }
}
