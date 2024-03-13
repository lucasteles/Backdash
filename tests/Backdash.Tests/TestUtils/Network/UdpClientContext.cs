using System.Net;
using Backdash.Core;
using Backdash.Network.Client;
using Backdash.Serialization;

namespace Backdash.Tests.TestUtils.Network;

sealed class UdpClientContext<T> : IDisposable where T : struct
{
    public UdpEventObserver<T> Observer { get; }
    public UdpClient<T> Client { get; }

    public IPEndPoint Loopback { get; }
    public SocketAddress Address { get; }

    public UdpClientContext(IBinarySerializer<T> serializer, int? port = null)
    {
        Observer = new();
        port ??= PortUtils.FindFreePort();
        UdpSocket socket = new(port.Value);
        Loopback = new(IPAddress.Loopback, port.Value);
        Address = Loopback.Serialize();

        Client = new UdpClient<T>(
            socket,
            serializer,
            Observer,
            Logger.CreateConsoleLogger(LogLevel.Off));
    }

    public int Port => Client.Port;
    public void Dispose() => Client.Dispose();
}
