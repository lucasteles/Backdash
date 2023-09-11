using nGGPO.Backends;

namespace nGGPO;

public class Session
{
    public static ISession<TInput, TGameState> Start<TInput, TGameState>(
        ISessionCallbacks<TGameState> cb, string game, int numPlayers, int localPort)
        where TInput : struct
        where TGameState : struct

    {
        return new Peer2PeerBackend<TInput, TGameState>(cb, game, localPort, numPlayers);
    }
}