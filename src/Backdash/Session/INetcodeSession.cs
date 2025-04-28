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
    ///     Returns the configured frame rate.
    /// </summary>
    int FixedFrameRate { get; }

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

    /// <summary>
    ///     Returns true if the session is in rollback state
    /// </summary>
    bool IsInRollback { get; }

    /// <summary>
    ///     Returns the last saved state.
    /// </summary>
    SavedFrame GetCurrentSavedFrame();

    /// <summary>
    ///     Returns the checksum of the current saved state.
    /// </summary>
    uint CurrentChecksum => GetCurrentSavedFrame().Checksum;

    /// <summary>
    ///     Returns the size of the current saved state.
    /// </summary>
    ByteSize CurrentStateSize => GetCurrentSavedFrame().Size;
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
    ///     Disconnects a remote player from a game.
    /// </summary>
    void DisconnectPlayer(NetcodePlayer player);

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
    PlayerConnectionStatus GetPlayerStatus(NetcodePlayer player);

    /// <summary>
    ///     Gets statistics and information about a player in <see cref="NetcodePlayer.NetworkStats"/>.
    ///     Returns <see langword="false" /> if the request player is not connected or synchronized.
    /// </summary>
    bool UpdateNetworkStats(NetcodePlayer player);

    /// <summary>
    ///     Change the amount of delay frames for local input.
    /// </summary>
    /// <param name="player"></param>
    /// <param name="delayInFrames"></param>
    void SetFrameDelay(NetcodePlayer player, int delayInFrames);

    /// <summary>
    ///     Load state for saved <paramref name="frame" />.
    /// </summary>
    /// <returns>true if succeeded.</returns>
    bool LoadFrame(Frame frame);

    ///     <inheritdoc cref="LoadFrame(Backdash.Frame)"/>
    bool LoadFrame(int frame) => LoadFrame(new Frame(frame));

    /// <summary>
    ///     Try to get the session <see cref="SessionReplayControl" />
    /// </summary>
    SessionReplayControl? ReplayController => null;

    /// <summary>
    ///     Returns a list of all input players in the session.
    /// </summary>
    IReadOnlySet<NetcodePlayer> GetPlayers();

    /// <summary>
    ///     Returns a list of all spectators in the session.
    /// </summary>
    IReadOnlySet<NetcodePlayer> GetSpectators();

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
    /// </summary>
    /// <param name="player"></param>
    /// <returns><see cref="ResultCode.Ok" /> if success.</returns>
    ResultCode AddPlayer(NetcodePlayer player);

    /// <summary>
    ///     Add a list of <see name="Player" /> into current session.
    /// </summary>
    /// <returns>An equivalent <see cref="ResultCode" /> list.</returns>
    IReadOnlyList<ResultCode> AddPlayers(IReadOnlyList<NetcodePlayer> players)
    {
        var result = new ResultCode[players.Count];
        for (var index = 0; index < players.Count; index++)
            result[index] = AddPlayer(players[index]);
        return result;
    }

    /// <summary>
    ///     Find player by unique ID
    /// </summary>
    /// <seealso cref="NetcodePlayer.Id"/>
    NetcodePlayer? FindPlayer(Guid id);

    /// <summary>
    ///     Tries to get first player of type <paramref name="playerType"/>
    /// </summary>
    bool TryGetPlayer(PlayerType playerType, [NotNullWhen(true)] out NetcodePlayer? player)
    {
        if (GetPlayers().Cast<NetcodePlayer?>().FirstOrDefault(p => p?.Type == playerType) is { } found)
        {
            player = found;
            return true;
        }

        player = null;
        return false;
    }

    /// <summary>
    ///     Tries to get first local player
    /// </summary>
    bool TryGetLocalPlayer([NotNullWhen(true)] out NetcodePlayer? player) => TryGetPlayer(PlayerType.Local, out player);

    /// <summary>
    ///     Tries to get first remote player
    /// </summary>
    bool TryGetRemotePlayer([NotNullWhen(true)] out NetcodePlayer? player) =>
        TryGetPlayer(PlayerType.Remote, out player);
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
    ///     This must be called once every frame for all players of type <see cref="PlayerType.Local" />.
    /// </summary>
    /// <param name="player">Player owner of the inputs</param>
    /// <param name="localInput">The input value</param>
    ResultCode AddLocalInput(NetcodePlayer player, in TInput localInput);

    /// <summary>
    ///     Synchronizes the inputs of the local and remote players into a local buffer.
    ///     You should call this before every frame of execution, including those frames which happen during rollback.
    /// </summary>
    ResultCode SynchronizeInputs();

    /// <summary>
    ///     Add an extra state seed to calculate the next <see cref="INetcodeRandom"/> on <see cref="Random"/>
    ///     This value state must be deterministic and be called every frame before <see cref="SynchronizeInputs"/>
    /// </summary>
    void SetRandomSeed(uint seed, uint extraState = 0);

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
    ref readonly SynchronizedInput<TInput> GetInput(NetcodePlayer player) =>
        ref CurrentSynchronizedInputs[player.Index];

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
