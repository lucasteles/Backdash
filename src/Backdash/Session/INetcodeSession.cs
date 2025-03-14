using System.Diagnostics.CodeAnalysis;
using Backdash.Data;
using Backdash.Network;
using Backdash.Synchronizing;
using Backdash.Synchronizing.Random;
using Backdash.Synchronizing.State;

namespace Backdash;

/// <summary>
///     Contract for managing a netcode session.
/// </summary>
public interface INetcodeSessionInfo
{
    /// <summary>
    ///     Returns the number of player in the current session.
    /// </summary>
    int NumberOfPlayers { get; }

    /// <summary>
    ///     Returns the number of spectators in the current session.
    /// </summary>
    int NumberOfSpectators { get; }

    /// <summary>
    ///     Returns the current session <see cref="Frame" />.
    /// </summary>
    Frame CurrentFrame { get; }

    /// <summary>
    ///     Returns the current <see cref="SessionMode" />.
    /// </summary>
    SessionMode Mode { get; }

    /// <summary>
    ///     Returns the number of current rollback frames.
    /// </summary>
    /// <seealso cref="FrameSpan" />
    FrameSpan RollbackFrames { get; }

    /// <summary>
    ///     Returns the number of frames the client is behind.
    /// </summary>
    /// <seealso cref="FrameSpan" />
    FrameSpan FramesBehind { get; }

    /// <summary>
    ///     Returns the current TCP local port.
    /// </summary>
    int LocalPort { get; }
}

/// <summary>
///     Contract for managing a netcode session.
/// </summary>
public interface INetcodeSession : INetcodeSessionInfo, IDisposable
{
    /// <summary>
    ///     Returns session info
    /// </summary>
    INetcodeSessionInfo GetInfo() => this;

    /// <summary>
    ///     Returns the checksum of the last saved state.
    /// </summary>
    SavedFrame GetCurrentSavedFrame();

    /// <summary>
    ///     Disconnects a remote player from a game.
    /// </summary>
    void DisconnectPlayer(in PlayerHandle player);

    /// <summary>
    ///     Should be called at the start of each frame of your application.
    /// </summary>
    void BeginFrame();

    /// <summary>
    ///     Should be called at the end of each frame of your application and also in
    ///     <see cref="INetcodeSessionHandler.AdvanceFrame" />.
    /// </summary>
    void AdvanceFrame();

    /// <summary>
    ///     Returns connection status of a player.
    /// </summary>
    PlayerConnectionStatus GetPlayerStatus(in PlayerHandle player);

    /// <summary>
    ///     Gets statistics and information about a player into <paramref name="info" />.
    ///     Returns <see langword="false" /> if the request player is not connected or synchronized.
    /// </summary>
    bool GetNetworkStatus(in PlayerHandle player, ref PeerNetworkStats info);

    /// <summary>
    ///     Change the amount of delay frames for local input.
    /// </summary>
    /// <param name="player"></param>
    /// <param name="delayInFrames"></param>
    void SetFrameDelay(PlayerHandle player, int delayInFrames);

    /// <summary>
    ///     Load state for saved <paramref name="frame" />.
    /// </summary>
    /// <returns>true if succeeded.</returns>
    bool LoadFrame(in Frame frame);

    /// <summary>
    ///     Try to get the session <see cref="SessionReplayControl" />
    /// </summary>
    SessionReplayControl? ReplayController => null;

    /// <summary>
    ///     Return true if the session is <see cref="SessionMode.Replay" />
    /// </summary>
    [MemberNotNullWhen(true, nameof(ReplayController))]
    bool IsReplay() => Mode is SessionMode.Replay;

    /// <summary>
    ///     Return true if the session is <see cref="SessionMode.Remote" />
    /// </summary>
    bool IsRemote() => Mode is SessionMode.Remote;

    /// <summary>
    ///     Return true if the session is <see cref="SessionMode.Spectator" />
    /// </summary>
    bool IsSpectator() => Mode is SessionMode.Spectator;

    /// <summary>
    ///     Return true if the session is <see cref="SessionMode.Local" />
    /// </summary>
    bool IsLocal() => Mode is SessionMode.Local;

    /// <summary>
    ///     Return true if the session is <see cref="SessionMode.SyncTest" />
    /// </summary>
    bool IsSyncTest() => Mode is SessionMode.SyncTest;

    /// <summary>
    ///     Add the <paramref name="player" /> into current session.
    ///     Usually an instance of <see cref="LocalPlayer" />, <see cref="RemotePlayer" /> or <see cref="Spectator" />.
    /// </summary>
    /// <param name="player"></param>
    /// <returns><see cref="ResultCode.Ok" /> if success.</returns>
    ResultCode AddPlayer(Player player);

    /// <summary>
    ///     Add a list of <see name="Player" /> into current session.
    ///     Usually instances of <see cref="LocalPlayer" />, <see cref="RemotePlayer" /> or <see cref="Spectator" />
    /// </summary>
    /// <returns>An equivalent <see cref="ResultCode" /> list.</returns>
    IReadOnlyList<ResultCode> AddPlayers(IReadOnlyList<Player> players);

    /// <summary>
    ///     Returns a list of all input players in the session.
    /// </summary>
    IReadOnlySet<PlayerHandle> GetPlayers();

    /// <summary>
    ///     Returns a list of all spectators in the session.
    /// </summary>
    IReadOnlySet<PlayerHandle> GetSpectators();

    /// <summary>
    ///     Tries to get first player of type <paramref name="playerType"/>
    /// </summary>
    bool TryGetPlayer(PlayerType playerType, out PlayerHandle player)
    {
        if (GetPlayers().Cast<PlayerHandle?>().FirstOrDefault(p => p?.Type == playerType) is { } found)
        {
            player = found;
            return true;
        }

        player = default;
        return true;
    }

    /// <summary>
    ///     Tries to get first local player
    /// </summary>
    bool TryGetLocalPlayer(out PlayerHandle player) => TryGetPlayer(PlayerType.Local, out player);

    /// <summary>
    ///     Tries to get first remote player
    /// </summary>
    bool TryGetRemotePlayer(out PlayerHandle player) => TryGetPlayer(PlayerType.Remote, out player);

    /// <summary>
    ///     Starts the background work for the session.
    ///     (Socket receiver, input queue, peer synchronization, etc.)
    /// </summary>
    void Start(CancellationToken stoppingToken = default);

    /// <summary>
    ///     Waits the session background work to finish.
    /// </summary>
    Task WaitToStop(CancellationToken stoppingToken = default);

    /// <summary>
    ///     Set the handler for the current session.
    ///     The client must call this before <see cref="Start" />.
    /// </summary>
    void SetHandler(INetcodeSessionHandler handler);
}

/// <summary>
///     Contract for managing a netcode session.
/// </summary>
/// <typeparam name="TInput">Game input type</typeparam>
public interface INetcodeSession<TInput> : INetcodeSession where TInput : unmanaged
{
    /// <summary>
    ///     Deterministic random value generator.
    ///     Must be called after <see cref="SynchronizeInputs" />.
    /// </summary>
    INetcodeRandom Random { get; }

    /// <summary>
    ///     Used add local inputs and notify the netcode that they should be transmitted to remote players.
    ///     This must be called once every frame for all player of type <see cref="PlayerType.Local" />.
    /// </summary>
    /// <param name="player">Player owner of the inputs</param>
    /// <param name="localInput">The input value</param>
    ResultCode AddLocalInput(PlayerHandle player, in TInput localInput);

    /// <summary>
    ///     Synchronizes the inputs of the local and remote players into a local buffer.
    ///     You should call this before every frame of execution, including those frames which happen during rollback.
    /// </summary>
    ResultCode SynchronizeInputs();

    /// <summary>
    ///     Return all synchronized inputs with connect status.
    ///     This must be called after <see cref="SynchronizeInputs" />
    /// </summary>
    ReadOnlySpan<SynchronizedInput<TInput>> CurrentSynchronizedInputs { get; }

    /// <summary>
    ///     Return all synchronized inputs.
    ///     This must be called after <see cref="SynchronizeInputs" />
    /// </summary>
    ReadOnlySpan<TInput> CurrentInputs { get; }


    /// <summary>
    ///     Returns the value of a synchronized input for the requested <paramref name="player" />.
    ///     This must be called after <see cref="SynchronizeInputs" />
    /// </summary>
    ref readonly SynchronizedInput<TInput> GetInput(in PlayerHandle player) =>
        ref CurrentSynchronizedInputs[player.InternalQueue];

    /// <summary>
    ///     Returns the value of a synchronized input for the requested player index.
    ///     This must be called after <see cref="SynchronizeInputs" />
    /// </summary>
    ref readonly SynchronizedInput<TInput> GetInput(int index) =>
        ref CurrentSynchronizedInputs[index];

    /// <summary>
    ///     Copy the value of all synchronized inputs into the <paramref name="buffer" />.
    ///     This must be called after <see cref="SynchronizeInputs" />
    /// </summary>
    void GetInputs(Span<SynchronizedInput<TInput>> buffer) => CurrentSynchronizedInputs.CopyTo(buffer);

    /// <summary>
    ///     Copy the value of all synchronized inputs into the <paramref name="buffer" />.
    ///     This must be called after <see cref="SynchronizeInputs" />
    /// </summary>
    void GetInputs(Span<TInput> buffer) => CurrentInputs.CopyTo(buffer);
}
