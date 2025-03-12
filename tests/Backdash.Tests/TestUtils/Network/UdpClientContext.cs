using System.Net;
using Backdash.Core;
using Backdash.Network;
using Backdash.Network.Client;
using Backdash.Serialization;

namespace Backdash.Tests.TestUtils.Network;

sealed class UdpClientContext<T> : IDisposable where T : struct
{
    public PeerEventObserver<T> Observer { get; }
    public PeerClient<T> Client { get; }

    public int Port { get; }
    public IPEndPoint Loopback { get; }
    public SocketAddress Address { get; }

    public UdpClientContext(IBinarySerializer<T> serializer, int? port = null)
    {
        Observer = new();
        port ??= NetUtils.FindFreePort();
        UdpSocket socket = new(port.Value);
        Loopback = new(IPAddress.Loopback, port.Value);
        Port = port.Value;
        Address = Loopback.Serialize();

        Client = new(
            socket,
            serializer,
            Observer,
            Logger.CreateConsoleLogger(LogLevel.None)
        );
    }

    public void Dispose() => Client.Dispose();
}
