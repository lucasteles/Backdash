namespace Backdash.Network;
public enum PlayerConnectionStatus : sbyte
{
    Unknown = -1,
    Local,
    Syncing,
    Connected,
    Disconnected,
}
