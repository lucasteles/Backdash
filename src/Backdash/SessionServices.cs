using Backdash.Core;
using Backdash.Network.Client;
using Backdash.Serialization;
using Backdash.Synchronizing.Input;
using Backdash.Synchronizing.Input.Confirmed;
using Backdash.Synchronizing.Random;
using Backdash.Synchronizing.State;

namespace Backdash;

/// <summary>
/// Session dependencies.
/// </summary>
/// <typeparam name="TInput">Input type</typeparam>
public sealed class SessionServices<TInput> where TInput : unmanaged
{
    /// <summary>
    /// Serializer for session input.
    /// </summary>
    public IBinarySerializer<TInput>? InputSerializer { get; set; }

    /// <summary>
    /// Checksum provider service for session state.
    /// Defaults to: Fletcher32 <see cref="Fletcher32ChecksumProvider"/>
    /// </summary>
    public IChecksumProvider? ChecksumProvider { get; set; }

    /// <summary>
    /// Log writer service for session.
    /// </summary>
    public ILogWriter? LogWriter { get; set; }

    /// <summary>
    /// Input generator service for session.
    /// </summary>
    public IInputGenerator<TInput>? InputGenerator { get; set; }

    /// <summary>
    /// State store service for session.
    /// </summary>
    public IStateStore? StateStore { get; set; }

    /// <summary>
    /// State store service for session.
    /// </summary>
    public IPeerSocketFactory? PeerSocketFactory { get; set; }

    /// <summary>
    /// Default random service
    /// </summary>
    public Random? Random { get; set; }

    /// <summary>
    /// Service to listen for confirmed inputs
    /// </summary>
    public IInputListener<TInput>? InputListener { get; set; }

    /// <summary>
    /// Service for random value generation in session
    /// Defaults to <see cref="XorSimdRandom"/>
    /// <seealso cref="XorShiftRandom"/>
    /// </summary>
    public IDeterministicRandom? DeterministicRandom { get; set; }
}
