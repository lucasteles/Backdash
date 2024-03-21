using Backdash.Core;
using Backdash.Serialization;

namespace Backdash.Network.Client;

/// <summary>
/// Create new instances of <see cref="IPeerClient{T}"/>
/// </summary>
public static class PeerClientFactory
{
    internal static IPeerClient<T> Create<T>(UdpSocket socket,
        IBinarySerializer<T> serializer,
        IPeerObserver<T> observer, Logger logger,
        int maxPacketSize = Max.UdpPacketSize
    ) where T : struct =>
        new PeerClient<T>(socket, serializer, observer, logger, maxPacketSize);

    /// <summary>
    ///  Creates new <see cref="IPeerClient{T}"/>
    /// </summary>
    public static IPeerClient<T> Create<T>(UdpSocket socket,
        IBinarySerializer<T> serializer,
        IPeerObserver<T> observer,
        int maxPacketSize = Max.UdpPacketSize
    ) where T : struct =>
        Create(socket, serializer, observer, Logger.CreateConsoleLogger(LogLevel.None), maxPacketSize);
}
