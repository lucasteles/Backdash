using System.Net;
using Backdash.Backends;
using Backdash.Core;
using Backdash.Data;
using Backdash.Network.Client;
using Backdash.Synchronizing;
using Backdash.Synchronizing.Input.Confirmed;
using Backdash.Synchronizing.State;

namespace Backdash;

/// <summary>
/// The session factory used to create new netcode sessions.
/// </summary>
///  <seealso cref="IRollbackSession{TInput}"/>
public static class RollbackNetcode
{
    /// <summary>
    /// Initializes new multiplayer session.
    /// </summary>
    /// <param name="port">The <see cref="UdpSocket"/> port</param>
    /// <param name="options">Session configuration</param>
    /// <param name="services">Session customizable dependencies</param>
    /// <typeparam name="TInput">Game input type</typeparam>
    public static IRollbackSession<TInput> CreateSession<TInput>(
        int port,
        RollbackOptions? options = null,
        SessionServices<TInput>? services = null
    )
        where TInput : unmanaged
    {
        options ??= new();
        return new Peer2PeerBackend<TInput>(port, options, BackendServices.Create(options, services));
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
    public static IRollbackSession<TInput> CreateSpectatorSession<TInput>(
        int port,
        IPEndPoint host,
        int numberOfPlayers,
        RollbackOptions? options = null,
        SessionServices<TInput>? services = null) where TInput : unmanaged
    {
        options ??= new();
        return new SpectatorBackend<TInput>(
            port, host, numberOfPlayers, options,
            BackendServices.Create(options, services));
    }

    /// <summary>
    /// Initializes new replay session.
    /// </summary>
    /// <param name="numberOfPlayers">Session player count</param>
    /// <param name="inputs">Inputs to be replayed</param>
    /// <param name="services">Session customizable dependencies</param>
    /// <param name="controls">replay control</param>
    /// <param name="useInputSeedForRandom"><see cref="RollbackOptions.UseInputSeedForRandom"/></param>
    /// <typeparam name="TInput">Game input type</typeparam>
    public static IRollbackSession<TInput> CreateReplaySession<TInput>(
        int numberOfPlayers,
        IReadOnlyList<ConfirmedInputs<TInput>> inputs,
        SessionServices<TInput>? services = null,
        SessionReplayControl? controls = null,
        bool useInputSeedForRandom = true)
        where TInput : unmanaged =>
        new ReplayBackend<TInput>(
            numberOfPlayers, useInputSeedForRandom, inputs,
            controls ?? new SessionReplayControl(),
            BackendServices.Create(new(), services));

    /// <summary>
    /// Initializes new sync test session.
    /// </summary>
    /// <param name="checkDistance">Total forced rollback frames.</param>
    /// <param name="options">Session configuration</param>
    /// <param name="services">Session customizable dependencies</param>
    /// <param name="desyncHandler">State de-sync handler</param>
    /// <param name="throwException">If true, throws on state de-synchronization.</param>
    /// <typeparam name="TInput">Game input type</typeparam>
    public static IRollbackSession<TInput> CreateSyncTestSession<TInput>(
        FrameSpan? checkDistance = null,
        RollbackOptions? options = null,
        SessionServices<TInput>? services = null,
        IStateDesyncHandler? desyncHandler = null,
        bool throwException = true
    )
        where TInput : unmanaged
    {
        options ??= new()
        {
            // ReSharper disable once RedundantArgumentDefaultValue
            Log = new(LogLevel.Information),
        };
        checkDistance ??= FrameSpan.One;
        return new SyncTestBackend<TInput>(
            options, checkDistance.Value, throwException,
            desyncHandler,
            BackendServices.Create(options, services)
        );
    }
}
