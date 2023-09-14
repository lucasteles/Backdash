using System;
using nGGPO.Backends;
using nGGPO.Serialization;

namespace nGGPO;

public class Session
{
    public static ISession<TInput, TGameState> Start<TInput, TGameState>(
        ISessionCallbacks<TGameState> cb,
        string game,
        int numPlayers,
        int localPort,
        IBinarySerializer<TInput>? inputSerializer = null
    )
        where TInput : struct
        where TGameState : struct
    {
        inputSerializer ??= GetSerializer<TInput>();

        return new Peer2PeerBackend<TInput, TGameState>(inputSerializer, cb, game, localPort,
            numPlayers);
    }

    public static IBinarySerializer<TInput> GetSerializer<TInput>() where TInput : struct
    {
        var inputType = typeof(TInput);
        if (inputType is {IsValueType: true, StructLayoutAttribute: not null})
            return new StructMarshalBinarySerializer<TInput>();

        throw new InvalidOperationException(
            $"Unable to infer serializer for type {typeof(TInput).FullName}");
    }
}