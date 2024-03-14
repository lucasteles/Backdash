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
        var ackMsg = "ack"u8.ToArray();

        var hostAddresses =
            string.IsNullOrWhiteSpace(settings.Value.UdpHost)
                ? []
                : await Dns.GetHostAddressesAsync(
                    settings.Value.UdpHost, AddressFamily.InterNetwork, stoppingToken);

        var bindAddress = hostAddresses.FirstOrDefault(IPAddress.Any);
        using Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Blocking = false;

        IPEndPoint bindEndpoint = new(bindAddress, port);
        logger.LogInformation("UDP: starting socket at {Endpoint}", bindEndpoint);
        socket.Bind(bindEndpoint);

        IPEndPoint anyEndPoint = new(IPAddress.Any, 0);

        var buffer = GC.AllocateArray<byte>(36, pinned: true);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var received = await socket
                    .ReceiveFromAsync(buffer, SocketFlags.None, anyEndPoint, stoppingToken)
                    .ConfigureAwait(false);

                if (received is not
                    { ReceivedBytes: var receivedSize, RemoteEndPoint: IPEndPoint remoteEndPoint })
                    continue;

                if (received.ReceivedBytes is 0)
                    continue;

                logger.LogInformation("UDP: Received {Size} bytes from {Endpoint}",
                    receivedSize, remoteEndPoint);

                if (!Guid.TryParse(Encoding.UTF8.GetString(buffer), out var peerToken))
                    continue;

                await socket.SendToAsync(ackMsg, SocketFlags.None, remoteEndPoint, stoppingToken);

                if (repository.FindEntry(peerToken) is not { } entry)
                    continue;

                if (entry.Peer.Endpoint is not null && !entry.Peer.Endpoint.Equals(remoteEndPoint))
                    logger.LogInformation(
                        "UDP: player {Name} changed address from {OldEndpoint} to {NewEndpoint}",
                        entry.Peer.Username, entry.Peer.Endpoint, remoteEndPoint);

                entry.Peer.Endpoint = remoteEndPoint;
                entry.LastRead = time.GetUtcNow();
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("UDP: operation cancelled");
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
