using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace nGGPO.Network;

public class Udp : PollSink, IDisposable
{
    protected UdpClient Socket;
    protected ICallbacks Callbacks;
    readonly IBinaryEncoder encoder;

    public Udp(IBinaryEncoder encoder, int bindingPort, ICallbacks callbacks)
    {
        Callbacks = callbacks;
        this.encoder = encoder;
        Logger.Info("binding udp socket to port {0}.\n", bindingPort);
        Socket = new(bindingPort);
    }


    public async Task SendTo(UdpMsg msg, IPEndPoint dest)
    {
        using var buffer = encoder.Encode(msg);
        var res = await Socket.SendAsync(buffer.Bytes, buffer.Bytes.Length, dest);
        if (res == (int) SocketError.SocketError)
        {
            Logger.Warn("Error sending socket value");
            return;
        }

        Logger.Info("sent packet length {0} to {1} (ret:{2}).\n",
            buffer.Bytes.Length, dest.ToString(), res);
    }


    public override async Task<bool> OnLoopPoll(object? cookie)
    {
        var data = await Socket.ReceiveAsync();
        var msg = encoder.Decode<UdpMsg>(data.Buffer);
        Callbacks.OnMsg(data.RemoteEndPoint, msg, data.Buffer.Length);

        return true;
    }

    public interface ICallbacks
    {
        void OnMsg(IPEndPoint from, UdpMsg msg, int len);
    }

    public void Dispose() => Socket.Dispose();
}