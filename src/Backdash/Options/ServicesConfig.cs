using Backdash.Core;
using Backdash.Network.Client;
using Backdash.Synchronizing.Input.Confirmed;
using Backdash.Synchronizing.Random;
using Backdash.Synchronizing.State;

namespace Backdash;

/// <summary>
///     Session dependencies.
/// </summary>
/// <typeparam name="TInput">Input type</typeparam>
[Serializable]
public sealed class ServicesConfig<TInput> where TInput : unmanaged
{
    /// <summary>
    ///     Checksum provider service for session state.
    ///     Defaults to: Fletcher32 <see cref="Fletcher32ChecksumProvider" />
    /// </summary>
    public IChecksumProvider? ChecksumProvider { get; set; }

    /// <summary>
    ///     Log writer service for session.
    /// </summary>
    public ILogWriter? LogWriter { get; set; }

    /// <summary>
    ///     State store service for session.
    /// </summary>
    public IStateStore? StateStore { get; set; }

    /// <summary>
    ///     State store service for session.
    /// </summary>
    public IPeerSocketFactory? PeerSocketFactory { get; set; }

    /// <summary>
    ///     Default internal random instance
    /// </summary>
    public Random? Random { get; set; }

    /// <summary>
    ///     Service for in-game random value generation in session
    ///     Defaults to <see cref="XorShiftRandom{T}" />
    /// </summary>
    public IDeterministicRandom<TInput>? DeterministicRandom { get; set; }

    /// <summary>
    ///     Service to listen for confirmed inputs
    /// </summary>
    public IInputListener<TInput>? InputListener { get; set; }

    /// <summary>
    ///     Comparer to be used with <typeparamref name="TInput" />
    /// </summary>
    public EqualityComparer<TInput>? InputComparer { get; set; }

    /// <inheritdoc cref="INetcodeSessionHandler"/>
    public INetcodeSessionHandler? SessionHandler { get; set; }
}
