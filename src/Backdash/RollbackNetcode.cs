using System.Net;
using Backdash.Backends;
using Backdash.Core;
using Backdash.Data;
using Backdash.Network.Client;
using Backdash.Sync.Input.Confirmed;

namespace Backdash;

/// <summary>
/// The session factory used to create new netcode sessions.
/// </summary>
///  <seealso cref="IRollbackSession{TInput}"/>
///  <seealso cref="IRollbackSession{TInput,TGameState}"/>
public static class RollbackNetcode
{
    /// <summary>
    /// Initializes new multiplayer session.
    /// </summary>
    /// <param name="port">The <see cref="UdpSocket"/> port</param>
    /// <param name="options">Session configuration</param>
    /// <param name="services">Session customizable dependencies</param>
    /// <typeparam name="TInput">Game input type</typeparam>
    /// <typeparam name="TGameState">Game state type</typeparam>
    public static IRollbackSession<TInput, TGameState> CreateSession<TInput, TGameState>(
        int port,
        RollbackOptions? options = null,
        SessionServices<TInput, TGameState>? services = null
    )
        where TInput : struct
        where TGameState : notnull, new()
    {
        options ??= new();
        return new Peer2PeerBackend<TInput, TGameState>(port, options, BackendServices.Create(options, services));
    }

    /// <summary>
    /// Initializes new spectator session.
    /// </summary>
    /// <param name="port">The <see cref="UdpSocket"/> port</param>
    /// <param name="host">The host <see cref="IPEndPoint"/> to be watched.</param>
    /// <param name="numberOfPlayers">Session player count</param>
    /// <param name="options">Session configuration</param>
    /// <param name="services">Session customizable dependencies</param>
    /// <typeparam name="TInput">Game input type</typeparam>
    /// <typeparam name="TGameState">Game state type</typeparam>
    public static IRollbackSession<TInput, TGameState> CreateSpectatorSession<TInput, TGameState>(
        int port,
        IPEndPoint host,
        int numberOfPlayers,
        RollbackOptions? options = null,
        SessionServices<TInput, TGameState>? services = null)
        where TInput : struct
        where TGameState : notnull, new()
    {
        options ??= new();
        return new SpectatorBackend<TInput, TGameState>(
            port, host, numberOfPlayers, options,
            BackendServices.Create(options, services));
    }

    /// <summary>
    /// Initializes new replay session.
    /// </summary>
    /// <param name="numberOfPlayers">Session player count</param>
    /// <param name="inputs">Inputs to be replayed</param>
    /// <param name="services">Session customizable dependencies</param>
    /// <typeparam name="TInput">Game input type</typeparam>
    /// <typeparam name="TGameState">Game state type</typeparam>
    public static IRollbackSession<TInput, TGameState> CreateReplaySession<TInput, TGameState>(
        int numberOfPlayers,
        IReadOnlyList<ConfirmedInputs<TInput>> inputs,
        SessionServices<TInput, TGameState>? services = null)
        where TInput : struct
        where TGameState : notnull, new() =>
        new ReplayBackend<TInput, TGameState>(
            numberOfPlayers, inputs,
            BackendServices.Create(new(), services));

    /// <summary>
    /// Initializes new sync test session.
    /// </summary>
    /// <param name="checkDistance">Total forced rollback frames.</param>
    /// <param name="options">Session configuration</param>
    /// <param name="services">Session customizable dependencies</param>
    /// <param name="throwException">If true, throws on state de-synchronization.</param>
    /// <typeparam name="TInput">Game input type</typeparam>
    /// <typeparam name="TGameState">Game state type</typeparam>
    public static IRollbackSession<TInput, TGameState> CreateSyncTestSession<TInput, TGameState>(
        FrameSpan? checkDistance = null,
        RollbackOptions? options = null,
        SessionServices<TInput, TGameState>? services = null,
        bool throwException = true
    )
        where TInput : struct
        where TGameState : notnull, new()
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
