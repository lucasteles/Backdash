using Backdash.Synchronizing;
using Backdash.Synchronizing.Input.Confirmed;

namespace Backdash.Options;

/// <summary>
///     Configurations for <see cref="INetcodeSession{TInput}" /> in <see cref="SessionMode.Replay" /> mode.
/// </summary>
public sealed record SessionReplayOptions<TInput> where TInput : unmanaged
{
    /// <summary>
    ///     Controller for replay session.
    /// </summary>
    public SessionReplayControl? ReplayController { get; set; }

    /// <summary>
    ///     Inputs to be replayed
    /// </summary>
    public IReadOnlyList<ConfirmedInputs<TInput>> InputList { get; set; } = [];
}
