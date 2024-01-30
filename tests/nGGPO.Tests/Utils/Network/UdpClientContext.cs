using System.Net;
using nGGPO.Network.Client;
using nGGPO.Serialization;

namespace nGGPO.Tests.Utils.Network;

sealed class UdpClientContext<T> : IDisposable where T : struct
{
    public UdpEventObserver<T> Observer { get; }
    public UdpClient<T> Socket { get; }

    public UdpClientContext(IBinarySerializer<T> serializer, int? port = null)
    {
        Observer = new();
        Socket = new UdpClient<T>(port ?? PortUtils.FindFreePort(), Observer, serializer, new ConsoleLogger
        {
            EnabledLevel = LogLevel.Trace
        });
    }

    public SocketAddress Address => Socket.Address;
    public int Port => Socket.Port;
    public void Dispose() => Socket.Dispose();
}
