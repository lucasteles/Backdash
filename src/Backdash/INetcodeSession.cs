using Backdash.Data;
using Backdash.Network;
using Backdash.Synchronizing.Random;

namespace Backdash;

/// <summary>
/// Holds basic session information.
/// </summary>
public interface INetcodeSessionInfo
{
    /// <summary>
    /// Returns the current session <see cref="Frame"/>
    /// </summary>
    Frame CurrentFrame { get; }

    /// <summary>
    /// Returns the current <see cref="SessionMode"/>
    /// </summary>
    SessionMode Mode { get; }

    /// <summary>
    /// Returns the number of current rollback frames. <seealso cref="FrameSpan"/>
    /// </summary>
    FrameSpan RollbackFrames { get; }

    /// <summary>
    /// Returns the number of frames the client is behind. <seealso cref="FrameSpan"/>
    /// </summary>
    FrameSpan FramesBehind { get; }
}

/// <summary>
/// Context for a multiplayer game session.
/// </summary>
/// <typeparam name="TInput">Game input type</typeparam>
public interface INetcodeSession<TInput> : INetcodeSessionInfo, IDisposable where TInput : unmanaged
{
    /// <summary>
    /// Returns the number of player in the current session
    /// </summary>
    int NumberOfPlayers { get; }

    /// <summary>
    /// Returns the number of spectators in the current session
    /// </summary>
    int NumberOfSpectators { get; }

    /// <summary>
    /// Deterministic random value generator.
    /// This must be called after <see cref="SynchronizeInputs"/>
    /// </summary>
    IDeterministicRandom Random { get; }

    /// <summary>
    /// Returns a list of all input players in the session.
    /// </summary>
    IReadOnlyCollection<PlayerHandle> GetPlayers();

    /// <summary>
    /// Returns a list of all spectators in the session.
    /// </summary>
    IReadOnlyCollection<PlayerHandle> GetSpectators();

    /// <summary>
    /// Disconnects a remote player from a game.
    /// </summary>
    void DisconnectPlayer(in PlayerHandle player);

    /// <summary>
    /// Used add local inputs and notify the netcode that they should be transmitted to remote players.
    /// This must be called once every frame for all player of type <see cref="PlayerType.Local"/>.
    /// </summary>
    /// <param name="player">Player owner of the inputs</param>
    /// <param name="localInput">The input value</param>
    ResultCode AddLocalInput(PlayerHandle player, in TInput localInput);

    /// <summary>
    /// Synchronizes the inputs of the local and remote players into a local buffer
    /// You should call this before every frame of execution, including those frames which happen during rollback.
    /// </summary>
    ResultCode SynchronizeInputs();

    /// <summary>
    /// Returns the value of a synchronized input for the requested <paramref name="player"/>.
    /// This must be called after <see cref="SynchronizeInputs"/>
    /// </summary>
    ref readonly SynchronizedInput<TInput> GetInput(in PlayerHandle player);

    /// <summary>
    /// Returns the value of a synchronized input for the requested player index.
    /// This must be called after <see cref="SynchronizeInputs"/>
    /// </summary>
    ref readonly SynchronizedInput<TInput> GetInput(int index);

    /// <summary>
    /// Copy the value of all synchronized inputs into the <paramref name="buffer"/>.
    /// This must be called after <see cref="SynchronizeInputs"/>
    /// </summary>
    void GetInputs(Span<SynchronizedInput<TInput>> buffer)
    {
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = GetInput(i);
    }

    /// <summary>
    /// Copy the value of all synchronized inputs into the <paramref name="buffer"/>.
    /// This must be called after <see cref="SynchronizeInputs"/>
    /// </summary>
    void GetInputs(Span<TInput> buffer)
    {
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = GetInput(i);
    }

    /// <summary>
    /// Should be called at the start of each frame of your application
    /// </summary>
    void BeginFrame();

    /// <summary>
    /// Should be called at the end of each frame of your application and also in <see cref="INetcodeSessionHandler.AdvanceFrame"/>.
    /// </summary>
    void AdvanceFrame();


    /// <summary>
    /// Returns connection status of a player.
    /// </summary>
    PlayerConnectionStatus GetPlayerStatus(in PlayerHandle player);

    /// <summary>
    /// Gets statistics and information about a player into <paramref name="info"/>.
    /// Returns <see langword="false"/> if the request player is not connected or synchronized.
    /// </summary>
    bool GetNetworkStatus(in PlayerHandle player, ref PeerNetworkStats info);

    /// <summary>
    /// Change the amount of delay frames for local input.
    /// </summary>
    /// <param name="player"></param>
    /// <param name="delayInFrames"></param>
    void SetFrameDelay(PlayerHandle player, int delayInFrames);

    /// <summary>
    /// Add the <paramref name="player"/> into current session.
    /// Usually an instance of <see cref="LocalPlayer"/>, <see cref="RemotePlayer"/> or <see cref="Spectator"/>
    /// </summary>
    /// <param name="player"></param>
    /// <returns><see cref="ResultCode.Ok"/> if success.</returns>
    ResultCode AddPlayer(Player player);

    /// <summary>
    /// Add a list of <see name="Player"/> into current session.
    /// Usually instances of <see cref="LocalPlayer"/>, <see cref="RemotePlayer"/> or <see cref="Spectator"/>
    /// </summary>
    /// <returns>A equivalent <see cref="ResultCode"/> list.</returns>
    IReadOnlyList<ResultCode> AddPlayers(IReadOnlyList<Player> players);

    /// <summary>
    /// Starts the background work for the session
    /// (Socket receiver, input queue, peer synchronization, etc.)
    /// </summary>
    void Start(CancellationToken stoppingToken = default);

    /// <summary>
    /// Waits the session background work to finish.
    /// </summary>
    Task WaitToStop(CancellationToken stoppingToken = default);

    /// <summary>
    /// Set the handler for the current session.
    /// The client must call this before <see cref="Start"/>.
    /// </summary>
    void SetHandler(INetcodeSessionHandler handler);
}
