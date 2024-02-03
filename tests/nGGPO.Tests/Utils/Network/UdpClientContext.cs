using System.Net;
using nGGPO.Network.Client;
using nGGPO.Serialization;

namespace nGGPO.Tests.Utils.Network;

sealed class UdpClientContext<T> : IDisposable where T : struct
{
    public UdpEventObserver<T> Observer { get; }
    public UdpClient<T> Client { get; }

    public UdpClientContext(IBinarySerializer<T> serializer, int? port = null)
    {
        Observer = new();
        UdpSocket socket = new(port ?? PortUtils.FindFreePort());
        Client = new UdpClient<T>(socket, Observer, serializer, new ConsoleLogger
        {
            EnabledLevel = LogLevel.Trace,
        });
    }

    public SocketAddress Address => Client.Address;
    public int Port => Client.Port;
    public void Dispose() => Client.Dispose();
}
