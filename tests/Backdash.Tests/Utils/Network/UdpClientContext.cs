using System.Net;
using Backdash.Core;
using Backdash.Network.Client;
using Backdash.Serialization;

namespace Backdash.Tests.Utils.Network;

sealed class UdpClientContext<T> : IDisposable where T : struct
{
    public UdpEventObserver<T> Observer { get; }
    public UdpClient<T> Client { get; }

    public UdpClientContext(IBinarySerializer<T> serializer, int? port = null)
    {
        Observer = new();
        UdpSocket socket = new(port ?? PortUtils.FindFreePort());

        Client = new UdpClient<T>(
            socket,
            serializer,
            Observer,
            new ConsoleLogger
            {
                EnabledLevel = LogLevel.Off,
            });
    }

    public SocketAddress Address => Client.Address;
    public int Port => Client.Port;
    public void Dispose() => Client.Dispose();
}
