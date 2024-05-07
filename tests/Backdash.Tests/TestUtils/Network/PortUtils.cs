using System.Net;
using System.Net.Sockets;

namespace Backdash.Tests.TestUtils.Network;

static class PortUtils
{
    public static int FindFreePort()
    {
        TcpListener? tcpListener = null;
        try
        {
            tcpListener = new TcpListener(IPAddress.Loopback, 0);
            tcpListener.Start();
            return ((IPEndPoint)tcpListener.LocalEndpoint).Port;
        }
        finally
        {
            tcpListener?.Stop();
        }
    }
}
