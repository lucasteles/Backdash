namespace Backdash.Network;

/// <summary>
///     Player Connection Status
/// </summary>
public enum PlayerConnectionStatus : sbyte
{
    /// <summary>Unknown or invalid player status</summary>
    Unknown = -1,

    /// <summary>Local player</summary>
    Local,

    /// <summary>Syncing remote player</summary>
    Syncing,

    /// <summary>Connected remote player</summary>
    Connected,

    /// <summary>Disconnected remote player</summary>
    Disconnected,
}
