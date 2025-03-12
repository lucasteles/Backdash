using Backdash.Synchronizing.State;

namespace Backdash.Options;

/// <summary>
/// Configurations for <see cref="INetcodeSession{TInput}"/> in <see cref="SessionMode.SyncTest"/> mode.
/// </summary>
public sealed record SyncTestOptions
{
    /// <summary>
    /// If true, throws on state de-synchronization.
    /// </summary>
    public bool ThrowOnDesync { get; set; } = true;

    /// <summary>
    /// Sets desync handler for <see cref="SessionMode.SyncTest"/> sessions.
    /// Useful for showing smart state diff.
    /// </summary>
    public IStateDesyncHandler? DesyncHandler { get; set; }

    /// <summary>
    /// Total forced rollback frames.
    /// </summary>
    /// <value>Defaults to <c>1</c></value>
    public int CheckDistance { get; set; } = 1;
}
