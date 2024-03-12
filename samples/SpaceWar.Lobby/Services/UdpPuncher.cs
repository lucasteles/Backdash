using System.Net;
using System.Net.Sockets;

namespace SpaceWar.Services;

public sealed class UdpPuncher : IDisposable
{
    readonly EndPoint remoteEndPoint;
    readonly Socket socket;
    readonly byte[] buffer = GC.AllocateArray<byte>(36, pinned: true);

    public UdpPuncher(int localPort, Uri serverUrl, int serverPort)
    {
        remoteEndPoint =
            new DnsEndPoint(serverUrl.DnsSafeHost, serverPort, AddressFamily.InterNetwork);

        socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
        {
            Blocking = false,
        };
        socket.Bind(new IPEndPoint(IPAddress.Any, localPort));
    }

    public async ValueTask Punch(Guid token, CancellationToken ct = default)
    {
        if (!token.TryFormat(buffer, out var bytesWritten) || bytesWritten is 0) return;
        await socket.SendToAsync(buffer.AsMemory()[..bytesWritten], remoteEndPoint, ct)
            .ConfigureAwait(false);
    }

    public void Dispose() => socket.Dispose();
}
