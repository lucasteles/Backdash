using System.Net;
using nGGPO.Data;

namespace nGGPO;

readonly struct LocalEndPoint(int playerNumber)
{
    public int PlayerNumber { get; } = playerNumber;
}

interface IRemote
{
    IPEndPoint EndPoint { get; }
}

public readonly record struct PlayerId(int Value)
{
    internal QueueIndex QueueNumber { get; } = new(Value - 1);
    public static PlayerId Empty { get; } = new(-1);
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
    public PlayerId Id { get; private set; } = new(playerNumber);
    internal QueueIndex QueueNumber => Id.QueueNumber;

    public static implicit operator PlayerId(Player player) => player.Id;

    public sealed class Local(int playerNumber) : Player(PlayerType.Local, playerNumber);

    public sealed class Remote(int playerNumber, IPEndPoint endpoint) : Player(PlayerType.Remote, playerNumber), IRemote
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
