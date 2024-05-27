namespace Backdash;

/// <summary>
/// Defines the mode of <see cref="IRollbackSession{TInput}"/>>
/// </summary>
public enum SessionMode
{
    /// <summary>Normal P2P match session</summary>
    Rollback,

    /// <summary>Spectator session</summary>
    Spectating,

    /// <summary>Replay session</summary>
    Replaying,

    /// <summary>Special sync test session</summary>
    SyncTest,
}
