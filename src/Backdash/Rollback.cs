using Backdash.Backends;
using Backdash.Core;
using Backdash.Network.Protocol;
using Backdash.Serialization;

namespace Backdash;

public static class Rollback
{
    public static IRollbackSession<TInput> CreateSession<TInput, TGameState>(
        IRollbackHandler<TGameState> callbacks,
        int numPlayers,
        int localPort,
        IBinarySerializer<TInput>? inputSerializer = null
    )
        where TInput : struct
        where TGameState : struct
        =>
            CreateSession(callbacks, new()
            {
                LocalPort = localPort,
                NumberOfPlayers = numPlayers,
            }, inputSerializer);

    public static IRollbackSession<TInput> CreateSession<TInput, TGameState>(
        IRollbackHandler<TGameState> callbacks,
        RollbackOptions options,
        IBinarySerializer<TInput>? inputSerializer = null,
        ILogger? logger = null
    )
        where TInput : struct
        where TGameState : struct
    {
        inputSerializer ??= BinarySerializerFactory.Get<TInput>(options.EnableEndianness)
                            ?? throw new InvalidOperationException(
                                $"Unable to infer serializer for type {typeof(TInput).FullName}");

        logger ??= new ConsoleLogger
        {
            EnabledLevel = options.LogLevel,
        };

        UdpClientFactory factory = new();

        return new Peer2PeerBackend<TInput, TGameState>(
            options,
            callbacks,
            inputSerializer,
            factory,
            new BackgroundJobManager(logger),
            logger
        );
    }
}
