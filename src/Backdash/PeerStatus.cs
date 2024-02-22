namespace Backdash;

public enum PlayerStatus : sbyte
{
    Unknown = -1,
    Local,
    Syncing,
    Connected,
    Disconnected,
}
