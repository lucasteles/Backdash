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

        var bindAddresses =
            !string.IsNullOrWhiteSpace(settings.Value.UdpHost)
                ? (await Dns.GetHostAddressesAsync(
                    settings.Value.UdpHost,
                    AddressFamily.InterNetwork,
                    stoppingToken))
                .DefaultIfEmpty(IPAddress.Any)
                : [IPAddress.Any];

        using Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        socket.Blocking = false;

        foreach (var bindAddress in bindAddresses)
        {
            IPEndPoint bindEndpoint = new(bindAddress, port);
            logger.LogInformation("UDP: starting socket at {Endpoint}", bindEndpoint);
            socket.Bind(bindEndpoint);
        }

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
                logger.LogInformation("UDP: Received {Size} bytes from {Endpoint}",
                    receivedSize, endpoint);

                if (!Guid.TryParse(Encoding.UTF8.GetString(buffer), out var peerToken))
                    continue;

                await socket.SendToAsync(ackMsg, SocketFlags.None, address, stoppingToken);

                if (repository.FindEntry(peerToken) is not { } entry)
                    continue;

                entry.Peer.Endpoint = endpoint;
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
