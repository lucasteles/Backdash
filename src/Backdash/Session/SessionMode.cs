namespace Backdash;

/// <summary>
/// Defines the mode of <see cref="INetcodeSession{TInput}"/>>
/// </summary>
public enum SessionMode : byte
{
    /// <summary>Normal P2P match session</summary>
    Remote,

    /// <summary>Spectator session</summary>
    Spectating,

    /// <summary>Replay session</summary>
    Replaying,

    /// <summary>Special sync test session</summary>
    SyncTest,

    /// <summary>Local only session</summary>
    Local,
}
