using System.Net;

namespace nGGPO.Types;

public enum PlayerType
{
    Local,
    Remote,
    Spectator,
}

public readonly struct LocalEndPoint
{
    public int PlayerNumber { get; }

    public LocalEndPoint(int playerNumber) => PlayerNumber = playerNumber;
}

public readonly record struct PlayerHandle(int Value)
{
    public static PlayerHandle Empty { get; } = new(-1);
}

public abstract class Player
{
    public abstract PlayerType Type { get; }
    public int PlayerNumber { get; }

    public PlayerHandle Handle { get; private set; } = PlayerHandle.Empty;

    internal void SetHandle(PlayerHandle handle) => Handle = handle;

    public static implicit operator PlayerHandle(Player player) => player.Handle;

    public Player(int playerNumber)
    {
        PlayerNumber = playerNumber;
    }

    public class Local : Player
    {
        public override PlayerType Type => PlayerType.Local;

        public Local(int playerNumber) : base(playerNumber)
        {
        }
    }

    public class Remote : Player
    {
        public override PlayerType Type => PlayerType.Remote;
        public IPEndPoint EndPoint { get; }

        public Remote(int playerNumber, IPEndPoint endpoint) : base(playerNumber) =>
            EndPoint = endpoint;

        public Remote(int playerNumber, IPAddress ipAddress, int port) : base(playerNumber) =>
            EndPoint = new IPEndPoint(ipAddress, port);
    }

    public class Spectator : Remote
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