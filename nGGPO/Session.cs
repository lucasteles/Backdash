using nGGPO.Backends;
using nGGPO.Network;
using nGGPO.Serialization;
using nGGPO.Serialization.Serializers;

namespace nGGPO;

public class Session
{
    public static ISession<TInput, TGameState> Start<TInput, TGameState>(
        ISessionCallbacks<TGameState> cb, string game, int numPlayers, int localPort)
        where TInput : struct
        where TGameState : struct
    {
        StructMarshalBinarySerializer serializer = new();
        return new Peer2PeerBackend<TInput, TGameState>(serializer, cb, game, localPort, numPlayers);
    }
}