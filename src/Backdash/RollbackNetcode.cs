using System.Net;
using Backdash.Backends;
using Backdash.Core;
using Backdash.Data;
namespace Backdash;
public static class RollbackNetcode
{
    public static IRollbackSession<TInput, TGameState> CreateSession<TInput, TGameState>(
        int port,
        RollbackOptions options,
        SessionServices<TInput, TGameState>? services = null
    )
        where TInput : struct
        where TGameState : IEquatable<TGameState>, new() =>
        new Peer2PeerBackend<TInput, TGameState>(port, options, BackendServices.Create(options, services));
    public static IRollbackSession<TInput, TGameState> CreateSpectatorSession<TInput, TGameState>(
        int port,
        IPEndPoint host,
        int numberOfPlayers,
        RollbackOptions options,
        SessionServices<TInput, TGameState>? services = null)
        where TInput : struct
        where TGameState : IEquatable<TGameState>, new() =>
        new SpectatorBackend<TInput, TGameState>(
            port, host, numberOfPlayers, options,
            BackendServices.Create(options, services));
    public static IRollbackSession<TInput, TGameState> CreateTestSession<TInput, TGameState>(
        FrameSpan? checkDistance = null,
        RollbackOptions? options = null,
        SessionServices<TInput, TGameState>? services = null,
        bool throwException = true
    )
        where TInput : struct
        where TGameState : IEquatable<TGameState>, new()
    {
        options ??= new()
        {
            // ReSharper disable once RedundantArgumentDefaultValue
            Log = new(LogLevel.Information),
        };
        checkDistance ??= FrameSpan.One;
        return new SyncTestBackend<TInput, TGameState>(
            options, checkDistance.Value, throwException,
            BackendServices.Create(options, services)
        );
    }
}
