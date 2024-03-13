using System.Net;
using System.Net.Sockets;
using System.Text;
using Backdash.Network.Client;

namespace SpaceWar.Services;

public sealed class UdpPuncher : IDisposable
{
    readonly IPEndPoint remoteEndPoint;
    readonly UdpSocket socket;
    readonly byte[] buffer = GC.AllocateArray<byte>(36, pinned: true);

    readonly CancellationTokenSource cts = new();
    public readonly HashSet<(EndPoint, Guid)> Received = [];

    bool disposed;

    public UdpPuncher(int localPort, Uri serverUrl, int serverPort)
    {
        var serverAddress =
            Dns.GetHostAddresses(serverUrl.DnsSafeHost, AddressFamily.InterNetwork).FirstOrDefault()
            ?? throw new InvalidOperationException($"Unable to get ip address from {serverUrl}");

        remoteEndPoint = new(serverAddress, serverPort);
        socket = new(localPort);

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
        var recBuffer = GC.AllocateArray<byte>(36, pinned: true);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var receiveInfo = await socket.ReceiveAsync(recBuffer, stoppingToken)
                    .ConfigureAwait(false);

                if (receiveInfo.ReceivedBytes is 0) continue;

                if (!Guid.TryParse(Encoding.UTF8.GetString(recBuffer), out var peerToken))
                    continue;

                Received.Add((receiveInfo.RemoteEndPoint, peerToken));
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception)
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
