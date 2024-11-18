using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using Backdash.Network.Client;
using SpaceWar.Models;

namespace SpaceWar.Services;

public sealed class LobbyUdpClient : IDisposable
{
    readonly IPEndPoint serverEndpoint;
    readonly UdpSocket socket;
    readonly CancellationTokenSource cts = new();
    readonly byte[] buffer = GC.AllocateArray<byte>(36, pinned: true);

    readonly HashSet<Guid> knownClients = [];
    bool disposed;

    public LobbyUdpClient(int localPort, Uri serverUrl, int serverPort)
    {
        var serverAddress = UdpSocket.GetDnsIpAddress(serverUrl.DnsSafeHost);
        serverEndpoint = new(serverAddress, serverPort);
        socket = new(localPort);

        Task.Run(() => Receive(cts.Token));
    }

    public async Task HandShake(User user, CancellationToken ct = default)
    {
        if (!user.Token.TryFormat(buffer, out var bytesWritten) || bytesWritten is 0) return;
        await socket.SendToAsync(buffer.AsMemory()[..bytesWritten], serverEndpoint, ct);
    }

    public async Task Ping(User user, Peer[] peers, CancellationToken ct = default)
    {
        if (peers.Length is 0 || !user.PeerId.TryFormat(buffer, out var bytesWritten) ||
            bytesWritten is 0)
            return;

        var msgBytes = buffer.AsMemory()[..bytesWritten];
        for (var i = 0; i < peers.Length; i++)
        {
            var peer = peers[i];
            if (peer.Connected && peer.PeerId != user.PeerId)
                await socket.SendToAsync(msgBytes, GetFallbackEndpoint(user, peer), ct);
        }
    }

    [AsyncMethodBuilder(typeof(PoolingAsyncValueTaskMethodBuilder))]
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

                knownClients.Add(peerToken);
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

    // Use local IP when over same network
    public IPEndPoint GetFallbackEndpoint(User user, Peer peer)
    {
        if (Equals(peer.Endpoint.Address, user.IP) && peer.LocalEndpoint is not null)
            return peer.LocalEndpoint;

        return peer.Endpoint;
    }

    public bool IsKnown(Guid id) => knownClients.Contains(id);

    public void Stop()
    {
        if (!cts.IsCancellationRequested)
            cts.Cancel();

        socket.Close();
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
