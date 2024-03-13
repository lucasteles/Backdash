using System.Net;
using System.Net.Sockets;
using System.Text;
using SpaceWar.Models;

namespace SpaceWar.Services;

public sealed class UdpPuncher : IDisposable
{
    readonly IPEndPoint remoteEndPoint;
    readonly Socket socket;
    readonly byte[] buffer = GC.AllocateArray<byte>(36, pinned: true);
    readonly HashSet<Guid> received = [];

    readonly CancellationTokenSource cts = new();
    bool disposed;

    public UdpPuncher(int localPort, Uri serverUrl, int serverPort)
    {
        var address = Dns.GetHostAddresses(
            serverUrl.DnsSafeHost, AddressFamily.InterNetwork).FirstOrDefault();

        if (address is null)
            throw new ArgumentException(nameof(serverUrl));

        remoteEndPoint = new(address, serverPort);
        socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
        {
            Blocking = false,

        };
        // socket.SetIPProtectionLevel(IPProtectionLevel.Unrestricted);
        socket.Bind(new IPEndPoint(IPAddress.Any, localPort));
        Task.Run(() => Receive(cts.Token));

    }

    public async Task Connect(Guid token,
        CancellationToken ct = default
    )
    {
        if (!token.TryFormat(buffer, out var bytesWritten) || bytesWritten is 0) return;
        await socket.SendToAsync(buffer.AsMemory()[..bytesWritten], remoteEndPoint, ct)
            .ConfigureAwait(false);
    }

    public async Task Punch(Guid token,
        IEnumerable<IPEndPoint> peers,
        CancellationToken ct = default
    )
    {
        if (!token.TryFormat(buffer, out var bytesWritten) || bytesWritten is 0) return;
        await Task.WhenAll(peers.Select(e =>
            socket.SendToAsync(buffer.AsMemory()[..bytesWritten], e, ct).AsTask()));
    }

    async ValueTask Receive(CancellationToken stoppingToken)
    {
        SocketAddress address = new(socket.AddressFamily);
        IPEndPoint endpoint = new(0, 0);

        var recBuffer = GC.AllocateArray<byte>(36, pinned: true);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var receivedSize = await socket
                    .ReceiveFromAsync(recBuffer, SocketFlags.None, address, stoppingToken)
                    .ConfigureAwait(false);

                if (receivedSize is 0) continue;
                endpoint = (IPEndPoint) endpoint.Create(address);

                if (!Guid.TryParse(Encoding.UTF8.GetString(recBuffer), out var peerToken))
                    continue;

                received.Add(peerToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                // skip
            }
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        socket.Dispose();
        cts.Cancel();
        cts.Dispose();
    }
}
