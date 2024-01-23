using System.Net;

namespace nGGPO;

readonly struct LocalEndPoint(int playerNumber)
{
    public int PlayerNumber { get; } = playerNumber;
}

interface IRemote
{
    IPEndPoint EndPoint { get; }
}

public readonly record struct PlayerHandle(int Value)
{
    public static PlayerHandle Empty { get; } = new(-1);
}

public enum PlayerType
{
    Local,
    Remote,
    Spectator,
}

public abstract class Player(PlayerType type, int playerNumber)
{
    public PlayerType Type { get; } = type;
    public int PlayerNumber { get; } = playerNumber;

    public PlayerHandle Handle { get; private set; } = PlayerHandle.Empty;

    internal void SetHandle(PlayerHandle handle) => Handle = handle;

    public static implicit operator PlayerHandle(Player player) => player.Handle;

    public sealed class Local(int playerNumber) : Player(PlayerType.Local, playerNumber);

    public class Remote(int playerNumber, IPEndPoint endpoint) : Player(PlayerType.Remote, playerNumber), IRemote
    {
        public IPEndPoint EndPoint { get; } = endpoint;

        public Remote(int playerNumber, IPAddress ipAddress, int port) :
            this(playerNumber, new IPEndPoint(ipAddress, port)) { }
    }

    public sealed class Spectator(int playerNumber, IPEndPoint endpoint)
        : Player(PlayerType.Spectator, playerNumber), IRemote
    {
        public IPEndPoint EndPoint { get; } = endpoint;

        public Spectator(int playerNumber, IPAddress ipAddress, int port) : this(playerNumber,
            new IPEndPoint(ipAddress, port)) { }
    }
}
