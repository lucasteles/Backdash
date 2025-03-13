namespace Backdash;

/// <summary>
///     Defines the mode of <see cref="INetcodeSession{TInput}" />>
/// </summary>
public enum SessionMode : byte
{
    /// <summary>Normal P2P match session</summary>
    Remote,

    /// <summary>Local only session</summary>
    Local,

    /// <summary>Spectator session</summary>
    Spectator,

    /// <summary>Replay session</summary>
    Replay,

    /// <summary>Special sync test session</summary>
    SyncTest,
}
