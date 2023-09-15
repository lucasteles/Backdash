using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using nGGPO.Serialization;
using nGGPO.Utils;

namespace nGGPO.Network;

class Udp : IPollLoopSink, IDisposable
{
    // TODO: go back to raw sockets
    readonly UdpClient socket;

    public delegate void OnMsgEvent(IPEndPoint from, in UdpMsg msg, int len);

    public event OnMsgEvent OnMsg = delegate { };

    readonly IBinarySerializer<UdpMsg> serializer = new StructMarshalBinarySerializer<UdpMsg>();

    public Udp(int bindingPort)
    {
        Logger.Info("binding udp socket to port {0}.\n", bindingPort);
        socket = new(bindingPort);
    }

    public async Task SendTo(UdpMsg msg, IPEndPoint dest)
    {
        using var buffer = serializer.Serialize(msg);

        var unnecessaryAllocationPleaseRemoveThis = buffer.Span.ToArray();
        var res = await socket.SendAsync(unnecessaryAllocationPleaseRemoveThis, buffer.Length,
            dest);

        if (res == (int) SocketError.SocketError)
        {
            Logger.Warn("Error sending socket value");
            return;
        }

        Logger.Info("sent packet length {0} to {1} (ret:{2}).\n",
            buffer.Length, dest.ToString(), res);
    }

    public async Task<bool> OnLoopPoll(object? cookie)
    {
        var data = await socket.ReceiveAsync();
        var msg = serializer.Deserialize(data.Buffer);
        OnMsg.Invoke(data.RemoteEndPoint, msg, data.Buffer.Length);

        return true;
    }

    public void Dispose() => socket.Dispose();
}