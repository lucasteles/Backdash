using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace nGGPO.Network;

class Udp : IPollLoopSink, IDisposable
{
    readonly UdpClient socket;
    readonly IBinaryEncoder encoder;

    public delegate void OnMsgEvent(IPEndPoint from, in UdpMsg msg, int len);

    public event OnMsgEvent OnMsg = delegate { };

    public Udp(IBinaryEncoder encoder, int bindingPort)
    {
        this.encoder = encoder;
        Logger.Info("binding udp socket to port {0}.\n", bindingPort);
        socket = new(bindingPort);
    }

    public async Task SendTo(UdpMsg msg, IPEndPoint dest)
    {
        using var buffer = encoder.Encode(msg);
        var res = await socket.SendAsync(buffer.Bytes, buffer.Bytes.Length, dest);
        if (res == (int) SocketError.SocketError)
        {
            Logger.Warn("Error sending socket value");
            return;
        }

        Logger.Info("sent packet length {0} to {1} (ret:{2}).\n",
            buffer.Bytes.Length, dest.ToString(), res);
    }


    public async Task<bool> OnLoopPoll(object? cookie)
    {
        var data = await socket.ReceiveAsync();
        var msg = encoder.Decode<UdpMsg>(data.Buffer);
        OnMsg.Invoke(data.RemoteEndPoint, msg, data.Buffer.Length);

        return true;
    }

    public void Dispose() => socket.Dispose();
}