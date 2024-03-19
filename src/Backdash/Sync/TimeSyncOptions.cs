namespace Backdash.Sync;

/// <summary>
/// Time Synchronization options
/// </summary>
public sealed class TimeSyncOptions
{
    /// <summary>
    /// Number of frames used for time synchronization
    /// </summary>
    public int FrameWindowSize { get; init; } = 40;

    /// <summary>
    /// Number of unique frames
    /// </summary>
    public int MinUniqueFrames { get; init; } = 10;

    /// <summary>
    /// Minimum required advantage to suggest synchronizing
    /// </summary>
    public int MinFrameAdvantage { get; init; } = 3;

    /// <summary>
    /// Max sync frames suggestion
    /// </summary>
    public int MaxFrameAdvantage { get; init; } = 9;
}
