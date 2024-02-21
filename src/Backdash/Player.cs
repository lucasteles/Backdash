using System.Net;
using System.Numerics;

namespace Backdash;

public abstract class Player : IEquatable<Player>, IEqualityOperators<Player, Player, bool>
{
    private protected Player(PlayerType type, int playerNumber) => Handle = new(type, playerNumber);
    public PlayerHandle Handle { get; internal set; }
    public PlayerType Type => Handle.Type;
    public int Number => Handle.Number;

    public bool IsSpectator() => Handle.IsSpectator();
    public bool IsRemote() => Handle.IsRemote();
    public bool IsLocal() => Handle.IsLocal();

    public sealed override string ToString() => Handle.ToString();
    public virtual bool Equals(Player? other) => Equals(this, other);
    public sealed override bool Equals(object? obj) => obj is Player player && Equals(player);
    public sealed override int GetHashCode() => HashCode.Combine(Type, Number);

    static bool Equals(Player? left, Player? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left == null || right == null) return false;
        return left.Handle.Equals(right.Handle);
    }

    public static bool operator ==(Player? left, Player? right) => Equals(left, right);
    public static bool operator !=(Player? left, Player? right) => !Equals(left, right);
}

public sealed class LocalPlayer(int playerNumber) : Player(PlayerType.Local, playerNumber);

public sealed class RemotePlayer(int playerNumber, IPEndPoint endpoint) : Player(PlayerType.Remote, playerNumber)
{
    public RemotePlayer(int playerNumber, IPAddress ipAddress, int port)
        : this(playerNumber, new IPEndPoint(ipAddress, port)) { }

    public IPEndPoint EndPoint { get; } = endpoint;
}

public sealed class Spectator(IPEndPoint endpoint) : Player(PlayerType.Spectator, 0)
{
    public Spectator(IPAddress ipAddress, int port) : this(new IPEndPoint(ipAddress, port)) { }

    public IPEndPoint EndPoint { get; } = endpoint;
}
