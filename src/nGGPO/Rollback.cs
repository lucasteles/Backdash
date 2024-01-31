using nGGPO.Backends;
using nGGPO.Serialization;

namespace nGGPO;

public static class Rollback
{
    public static IRollbackSession<TInput> CreateSession<TInput, TGameState>(
        ISessionCallbacks<TGameState> cb,
        int numPlayers,
        int localPort,
        IBinarySerializer<TInput>? inputSerializer = null
    )
        where TInput : struct
        where TGameState : struct
        =>
            CreateSession(cb, new()
            {
                LocalPort = localPort,
                NumberOfPlayers = numPlayers,
            }, inputSerializer);

    public static IRollbackSession<TInput> CreateSession<TInput, TGameState>(
        ISessionCallbacks<TGameState> cb,
        RollbackOptions options,
        IBinarySerializer<TInput>? inputSerializer = null
    )
        where TInput : struct
        where TGameState : struct
    {
        inputSerializer ??= BinarySerializerFactory.Get<TInput>()
                            ?? throw new InvalidOperationException(
                                $"Unable to infer serializer for type {typeof(TInput).FullName}");

        return new Peer2PeerBackend<TInput, TGameState>(
            inputSerializer,
            cb,
            options
        );
    }
}
