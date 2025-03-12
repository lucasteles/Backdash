using System.Net;
using System.Net.Sockets;

namespace Backdash.Network;

/// <summary>
/// Network utilities
/// </summary>
public static class NetUtils
{
    /// <summary>
    /// Finds a free TCP port.
    /// </summary>
    public static int FindFreePort()
    {
        TcpListener? tcpListener = null;
        try
        {
            tcpListener = new(IPAddress.Loopback, 0);
            tcpListener.Start();
            return ((IPEndPoint)tcpListener.LocalEndpoint).Port;
        }
        finally
        {
            tcpListener?.Stop();
        }
    }
}
