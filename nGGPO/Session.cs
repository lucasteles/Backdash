using nGGPO.Backends;
using nGGPO.Network;

namespace nGGPO;

public class Session
{
    public static ISession<TInput, TGameState> Start<TInput, TGameState>(
        ISessionCallbacks<TGameState> cb, string game, int numPlayers, int localPort)
        where TInput : struct
        where TGameState : struct
    {
        StructBinaryEncoder encoder = new();
        return new Peer2PeerBackend<TInput, TGameState>(encoder, cb, game, localPort, numPlayers);
    }
}