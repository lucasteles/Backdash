using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.Json;
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
        IBinarySerializer<TInput>? inputSerializer = null,
        JsonSerializerOptions? options = null
    )
        where TInput : struct
        where TGameState : struct
    {
        inputSerializer ??= GetSerializer<TInput>();

        return new Peer2PeerBackend<TInput, TGameState>(inputSerializer, cb, game, localPort,
            numPlayers);
    }

    public static IBinarySerializer<TInput> GetSerializer<TInput>(
        JsonSerializerOptions? options = null) where TInput : notnull
    {
        var inputType = typeof(TInput);
        if (inputType is {IsValueType: true, StructLayoutAttribute: not null})
            return new StructMarshalBinarySerializer<TInput>();

        return new JsonBinarySerializer<TInput>(options);
    }
}