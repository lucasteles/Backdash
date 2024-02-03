using nGGPO.Backends;
using nGGPO.Core;
using nGGPO.Network.Client;
using nGGPO.Network.Messages;
using nGGPO.Serialization;

namespace nGGPO;

public static class Rollback
{
    public static IRollbackSession<TInput> CreateSession<TInput, TGameState>(
        ISessionCallbacks<TGameState> callbacks,
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
        ISessionCallbacks<TGameState> callbacks,
        RollbackOptions options,
        IBinarySerializer<TInput>? inputSerializer = null,
        ILogger? logger = null
    )
        where TInput : struct
        where TGameState : struct
    {
        inputSerializer ??= BinarySerializerFactory.Get<TInput>()
                            ?? throw new InvalidOperationException(
                                $"Unable to infer serializer for type {typeof(TInput).FullName}");

        logger ??= new ConsoleLogger
        {
            EnabledLevel = options.LogLevel,
        };

        UdpObservableClient<ProtocolMessage> udpClient = new(
            options.LocalPort, new ProtocolMessageBinarySerializer(), logger
        );

        return new Peer2PeerBackend<TInput, TGameState>(
            options,
            callbacks,
            inputSerializer,
            udpClient,
            new BackgroundJobManager(logger),
            logger
        );
    }
}
