using System.Net;
using nGGPO.Network;
using nGGPO.Serialization;

namespace nGGPO.Tests.Utils.Network;

public sealed record UdpClientContext<T>(
    UdpPeerClient<T> Socket,
    IPEndPoint EndPoint
)
    : IDisposable
    where T : struct
{
    public UdpClientContext(UdpPeerClient<T> socket, IPAddress address) :
        this(socket, new IPEndPoint(address, socket.Port))
    {
    }

    public UdpClientContext(IBinarySerializer<T> serializer, int? port = null) :
        this(
            new UdpPeerClient<T>(
                port ?? PortUtils.FindFreePort(),
                serializer
            ),
            IPAddress.Loopback
        )
    {
    }

    public SocketAddress Address { get; } = EndPoint.Serialize();
    public int Port => EndPoint.Port;

    public void Dispose() => Socket.Dispose();
}
