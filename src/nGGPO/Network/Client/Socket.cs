using System.Net;
using System.Net.Sockets;

namespace nGGPO.Network.Client;

static class SocketFactory
{
    public static Socket Create(int port, ILogger logger)
    {
        if (port is < IPEndPoint.MinPort or > IPEndPoint.MaxPort)
            throw new ArgumentOutOfRangeException(nameof(port));

        Socket newSocket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
        {
            ExclusiveAddressUse = false,
            Blocking = false,
        };

        IPEndPoint localEp = new(IPAddress.Any, port);
        newSocket.Bind(localEp);
        logger.Info($"binding udp socket to port {port}.\n");

        return newSocket;
    }
}
