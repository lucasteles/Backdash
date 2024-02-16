namespace Backdash.Core;

public static class Max
{
    public const int RemoteConnections = 4;
    public const int LocalPlayers = 1;
    public const int TotalPlayers = RemoteConnections * LocalPlayers;
    public const int InputSizeInBytes = 8;
    public const int TotalInputSizeInBytes = InputSizeInBytes * LocalPlayers;
    public const int NumberOfSpectators = 32;
    public const int CompressedBytes = 512;
    public const int UdpPacketSize = 65_527;
}
