using System.Net;
using Backdash.Data;

namespace Backdash;

readonly struct LocalEndPoint(int playerNumber)
{
    public int PlayerNumber { get; } = playerNumber;
}

interface IRemote
{
    IPEndPoint EndPoint { get; }
}

public readonly record struct PlayerIndex(int Value)
{
    internal QueueIndex QueueNumber { get; } = new(Value - 1);
    public static PlayerIndex Empty { get; } = new(-1);
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
    public PlayerIndex Index { get; private set; } = new(playerNumber);
    internal QueueIndex QueueNumber => Index.QueueNumber;

    public static implicit operator PlayerIndex(Player player) => player.Index;

    public sealed class Local(int playerNumber) : Player(PlayerType.Local, playerNumber);

    public sealed class Remote(int playerNumber, IPEndPoint endpoint) : Player(PlayerType.Remote, playerNumber), IRemote
    {
        public IPEndPoint EndPoint { get; } = endpoint;

        public Remote(int playerNumber, IPAddress ipAddress, int port) :
            this(playerNumber, new IPEndPoint(ipAddress, port))
        { }
    }

    public sealed class Spectator(int playerNumber, IPEndPoint endpoint)
        : Player(PlayerType.Spectator, playerNumber), IRemote
    {
        public IPEndPoint EndPoint { get; } = endpoint;

        public Spectator(int playerNumber, IPAddress ipAddress, int port) : this(playerNumber,
            new IPEndPoint(ipAddress, port))
        { }
    }
}
