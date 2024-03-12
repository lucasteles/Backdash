using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Options;

namespace LobbyServer;

public class UdpListenerService(
    LobbyRepository repository,
    TimeProvider time,
    IOptions<AppSettings> settings,
    ILogger<UdpListenerService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var port = settings.Value.UdpPort;
        logger.LogInformation("UDP: starting socket at {Port}", port);

        using Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Blocking = false;

        EndPoint bindEndpoint =
            string.IsNullOrWhiteSpace(settings.Value.UdpHost)
                ? new IPEndPoint(IPAddress.Any, port)
                : new DnsEndPoint(settings.Value.UdpHost, port, AddressFamily.InterNetwork);

        socket.Bind(bindEndpoint);

        SocketAddress address = new(socket.AddressFamily);
        IPEndPoint endpoint = new(0, 0);

        var buffer = GC.AllocateArray<byte>(36, pinned: true);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var receivedSize = await socket
                    .ReceiveFromAsync(buffer, SocketFlags.None, address, stoppingToken)
                    .ConfigureAwait(false);

                if (receivedSize is 0) continue;
                endpoint = (IPEndPoint) endpoint.Create(address);
                logger.LogInformation("UDP: New request from {Endpoint}", endpoint);

                if (!Guid.TryParse(Encoding.UTF8.GetString(buffer), out var peerToken))
                    continue;

                if (repository.FindEntry(peerToken) is not { } entry) return;
                entry.Peer.Endpoint = endpoint;
                entry.LastRead = time.GetUtcNow();
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "UDP: socket error");
            }
        }

        logger.LogInformation("UDP: stopping");
    }
}
