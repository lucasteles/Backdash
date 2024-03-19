namespace Backdash.Sync;

/// <summary>
/// Time Synchronization options
/// </summary>
public sealed class TimeSyncOptions
{
    /// <summary>
    /// Number of frames used for time synchronization
    /// </summary>
    /// <value>Defaults to <c>40</c></value>
    public int FrameWindowSize { get; init; } = 40;

    /// <summary>
    /// Number of unique frames
    /// </summary>
    /// <value>Defaults to <c>10</c></value>
    public int MinUniqueFrames { get; init; } = 10;

    /// <summary>
    /// Minimum required advantage to recommend synchronization.
    /// Some things just aren't worth correcting for.
    /// Make sure the difference is relevant before proceeding.
    /// </summary>
    /// <value>Defaults to <c>3</c></value>
    public int MinFrameAdvantage { get; init; } = 3;

    /// <summary>
    /// Max sync recommendation frames.
    /// </summary>
    /// <value>Defaults to <c>9</c></value>
    public int MaxFrameAdvantage { get; init; } = 9;

    /// <summary>
    /// Make sure our input had been "idle enough" before recommending
    /// a sleep. This tries to make the emulator sleep while the
    /// user's input isn't sweeping in arcs (e.g. fireball motions in
    /// Street Fighter), which could cause the player to miss moves.
    /// </summary>
    /// <value>Defaults to <c>false</c></value>
    public bool RequireIdleInput { get; init; }
}
