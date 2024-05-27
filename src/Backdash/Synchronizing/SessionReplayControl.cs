namespace Backdash.Synchronizing;

/// <summary>
/// Control flow of a replay session.
/// <seealso cref="RollbackNetcode.CreateReplaySession{TInput,TGameState}"/>
/// </summary>
public sealed class SessionReplayControl
{
    /// <summary>
    /// Maximum number of frames for backward play
    /// Defaults to 300 (5 seconds in 60 fps)
    /// </summary>
    public int MaxBackwardFrames { get; init; } = 60 * 5;

    /// <summary>
    /// true if replay will flow backwards
    /// </summary>
    public bool IsBackward { get; set; }

    /// <summary>
    /// true if replay is paused
    /// </summary>
    public bool IsPaused { get; private set; }

    /// <summary>
    /// Pause replay. <seealso cref="IsPaused"/>
    /// </summary>
    public void Pause() => IsPaused = true;

    /// <summary>
    /// Toggle replay pause. <seealso cref="IsPaused"/>
    /// </summary>
    public void TogglePause() => IsPaused = !IsPaused;

    /// <summary>
    /// Toggle replay backward. <seealso cref="IsBackward"/>
    /// </summary>
    public void ToggleBackwards() => IsBackward = !IsBackward;

    /// <summary>
    /// Unpause replay. <seealso cref="IsPaused"/>
    /// </summary>
    public void Play(bool backwards = false)
    {
        IsPaused = false;
        IsBackward = backwards;
    }
}
