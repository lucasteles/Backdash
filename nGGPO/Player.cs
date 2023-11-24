using System.Net;

namespace nGGPO;

public enum PlayerType
{
    Local,
    Remote,
    Spectator,
}

public readonly struct LocalEndPoint(int playerNumber)
{
    public int PlayerNumber { get; } = playerNumber;
}

public readonly record struct PlayerHandle(int Value)
{
    public static PlayerHandle Empty { get; } = new(-1);
}

public abstract class Player(int playerNumber)
{
    public abstract PlayerType Type { get; }
    public int PlayerNumber { get; } = playerNumber;

    public PlayerHandle Handle { get; private set; } = PlayerHandle.Empty;

    internal void SetHandle(PlayerHandle handle) => Handle = handle;

    public static implicit operator PlayerHandle(Player player) => player.Handle;

    public class Local(int playerNumber) : Player(playerNumber)
    {
        public override PlayerType Type => PlayerType.Local;
    }

    public class Remote : Player
    {
        public override PlayerType Type => PlayerType.Remote;
        public IPEndPoint EndPoint { get; }

        public Remote(int playerNumber, IPEndPoint endpoint) : base(playerNumber) =>
            EndPoint = endpoint;

        public Remote(int playerNumber, IPAddress ipAddress, int port)
            : this(playerNumber, new IPEndPoint(ipAddress, port))
        {
        }
    }

    public sealed class Spectator : Remote
    {
        public override PlayerType Type => PlayerType.Spectator;

        public Spectator(int playerNumber, IPEndPoint endpoint) : base(playerNumber, endpoint)
        {
        }

        public Spectator(int playerNumber, IPAddress ipAddress, int port) : base(playerNumber,
            ipAddress, port)
        {
        }
    }
}