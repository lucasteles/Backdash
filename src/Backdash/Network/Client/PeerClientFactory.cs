using Backdash.Core;
using Backdash.Serialization;
using Backdash.Serialization.Internal;

namespace Backdash.Network.Client;

/// <summary>
/// Create new instances of <see cref="IPeerClient{T}"/>
/// </summary>
public static class PeerClientFactory
{
    /// <summary>
    ///  Creates new <see cref="IPeerClient{T}"/>
    /// </summary>
    public static IPeerClient<T> Create<T>(
        IPeerSocket socket,
        IBinarySerializer<T> serializer,
        IPeerObserver<T> observer,
        int maxPacketSize = Max.UdpPacketSize,
        LogLevel logLevel = LogLevel.None,
        ILogWriter? logWriter = null,
        DelayStrategy delayStrategy = DelayStrategy.Gaussian,
        Random? random = null
    ) where T : unmanaged => new PeerClient<T>(
        socket,
        serializer,
        observer,
        Logger.CreateConsoleLogger(logLevel, logWriter),
        new Clock(),
        DelayStrategyFactory.Create(new DefaultRandomNumberGenerator(random ?? Random.Shared), delayStrategy),
        maxPacketSize
    );

#if !AOT_ENABLED
    /// <summary>
    ///  Creates new <see cref="IPeerClient{T}"/>
    /// </summary>
    public static IPeerClient<T> Create<T>(
        IPeerSocket socket,
        IPeerObserver<T> observer,
        int maxPacketSize = Max.UdpPacketSize,
        LogLevel logLevel = LogLevel.None,
        ILogWriter? logWriter = null,
        DelayStrategy delayStrategy = DelayStrategy.Gaussian,
        Random? random = null
    ) where T : unmanaged => Create(
        socket,
        BinarySerializerFactory.FindOrThrow<T>(),
        observer,
        maxPacketSize,
        logLevel,
        logWriter,
        delayStrategy,
        random
    );
#endif
}
