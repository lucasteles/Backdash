using System.Net;
using System.Net.Sockets;
using System.Text;
using Backdash.Network.Client;
using SpaceWar.Models;

namespace SpaceWar.Services;

public sealed class UdpPuncher : IDisposable
{
    readonly IPEndPoint remoteEndPoint;
    readonly UdpSocket socket;
    readonly CancellationTokenSource cts = new();
    readonly byte[] buffer = GC.AllocateArray<byte>(36, pinned: true);

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

    public async Task HandShake(Guid token, CancellationToken ct = default)
    {
        if (!token.TryFormat(buffer, out var bytesWritten) || bytesWritten is 0) return;
        await socket.SendToAsync(buffer.AsMemory()[..bytesWritten], remoteEndPoint, ct)
            .ConfigureAwait(false);
    }

    public async Task Ping(User user, Peer[] peers, CancellationToken ct = default)
    {
        if (peers.Length is 0 || !user.Token.TryFormat(buffer, out var bytesWritten) ||
            bytesWritten is 0)
            return;

        var msgBytes = buffer.AsMemory()[..bytesWritten];
        for (var i = 0; i < peers.Length; i++)
        {
            var peer = peers[i];
            if (peer.Connected && peer.PeerId != user.PeerId)
                await socket.SendToAsync(msgBytes, peer.Endpoint, ct);
        }
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

                var msg = Encoding.UTF8.GetString(recBuffer);
                Console.WriteLine($"recv: {msg} from {receiveInfo.RemoteEndPoint}");
                if (!Guid.TryParse(msg, out var peerToken))
                    continue;

                Console.WriteLine($"Ping: from {peerToken} at {receiveInfo.RemoteEndPoint}");
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

    public void Stop()
    {
        if (!cts.IsCancellationRequested)
            cts.Cancel();
    }

    public void Dispose()
    {
        Stop();
        if (disposed) return;
        disposed = true;
        socket.Dispose();
        cts.Dispose();
    }
}
