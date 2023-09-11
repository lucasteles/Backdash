using System.Net;
using System.Net.Sockets;
using nGGPO.Types;

namespace nGGPO.Network;

public class Udp : PollSink
{
    protected Socket Socket;
    protected ICallbacks Callbacks;
    protected Poll Poll;

    public Udp(int port, Poll poll, ICallbacks callbacks)
    {
        Callbacks = callbacks;
        Poll = poll;
        poll.RegisterLoop(this);

        Logger.Info("binding udp socket to port {0}.\n", port);
        Socket = CreateSocket(port);
    }

    readonly byte[] iMode = {1, 0, 0, 0};
    readonly byte[] optOut = new byte[4];

    Socket CreateSocket(int bindPort)
    {
        const int optval = 1;
        Socket socket = new(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Unspecified);
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, optval);
        socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, optval);
        socket.IOControl(IOControlCode.NonBlockingIO, iMode, optOut);

        try
        {
            IPEndPoint ip = new(IPAddress.Any, bindPort);
            socket.Bind(ip);
            return socket;
        }
        catch
        {
            socket.Close();
            throw;
        }
    }

    public async Task SendTo(
        UdpMsg msg, EndPoint dst,
        SocketFlags flags = SocketFlags.None)
    {
        var buffer = Serializer.Encode(msg);

        var res = await Socket.SendToAsync(new(buffer), flags, dst);
        if (res == (int) SocketError.SocketError)
        {
            Logger.Warn("Error sending socket value");
            return;
        }

        Logger.Info("sent packet length {0} to {1} (ret:{2}).\n",
            buffer.Length, dst.ToString(), res);
    }


    public override async Task<bool> OnLoopPoll(object? cookie)
    {
        ArraySegment<byte> recvBuf = new(new byte[Max.UdpPacketSize]);

        IPEndPoint ip = new(IPAddress.Any, IPEndPoint.MinPort);
        var len = await Socket.ReceiveFromAsync(recvBuf, SocketFlags.None, ip);

        var msg = Serializer.Decode<UdpMsg>(recvBuf.Array!);
        Callbacks.OnMsg(Socket, msg, len.ReceivedBytes);

        return true;
    }

    public interface ICallbacks
    {
        void OnMsg(Socket from, UdpMsg msg, int len);
    }
}