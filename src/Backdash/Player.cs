using System.Net;
using System.Numerics;

namespace Backdash;

/// <summary>
/// Holds data of a player to be added to <see cref="INetcodeSession{TInput}"/>.
/// </summary>
[Serializable]
public abstract class Player : IEquatable<Player>, IEqualityOperators<Player, Player, bool>
{
    private protected Player(PlayerType type, int playerNumber) => Handle = new(type, playerNumber);

    /// <summary>
    /// Player handler, used to identify any player in session.
    /// </summary>
    public PlayerHandle Handle { get; internal set; }

    /// <inheritdoc cref="PlayerHandle.Type"/>
    public PlayerType Type => Handle.Type;

    /// <inheritdoc cref="PlayerHandle.Number"/>
    public int Number => Handle.Number;

    /// <inheritdoc cref="PlayerHandle.IsSpectator()"/>
    public bool IsSpectator() => Handle.IsSpectator();

    /// <inheritdoc cref="PlayerHandle.IsRemote()"/>
    public bool IsRemote() => Handle.IsRemote();

    /// <inheritdoc cref="PlayerHandle.IsLocal()"/>
    public bool IsLocal() => Handle.IsLocal();

    /// <inheritdoc cref="PlayerHandle.ToString()"/>
    public sealed override string ToString() => Handle.ToString();

    /// <inheritdoc/>
    public virtual bool Equals(Player? other) => Equals(this, other);

    /// <inheritdoc/>
    public sealed override bool Equals(object? obj) => obj is Player player && Equals(player);

    /// <inheritdoc/>
    public sealed override int GetHashCode() => HashCode.Combine(Type, Number);

    static bool Equals(Player? left, Player? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left == null || right == null) return false;
        return left.Handle.Equals(right.Handle);
    }

    /// <inheritdoc/>
    public static bool operator ==(Player? left, Player? right) => Equals(left, right);

    /// <inheritdoc/>
    public static bool operator !=(Player? left, Player? right) => !Equals(left, right);
}

/// <summary>
/// Holds data for a new player of type <see cref="PlayerType.Local"/>.
/// </summary>
/// <param name="playerNumber">Player number (starting from <c>1</c>)</param>
[Serializable]
public sealed class LocalPlayer(int playerNumber) : Player(PlayerType.Local, playerNumber);

/// <summary>
/// Holds data for a new player of type <see cref="PlayerType.Remote"/>.
/// </summary>
/// <param name="playerNumber">Player number (starting from <c>1</c>)</param>
/// <param name="endpoint">Player IP Endpoint <see cref="IPEndPoint"/></param>
[Serializable]
public sealed class RemotePlayer(int playerNumber, IPEndPoint endpoint) : Player(PlayerType.Remote, playerNumber)
{
    /// <summary>
    /// Initialize new <see cref="RemotePlayer"/>
    /// </summary>
    /// <param name="playerNumber">Player number</param>
    /// <param name="ipAddress">Player IP Address</param>
    /// <param name="port">Player remote port number</param>
    public RemotePlayer(int playerNumber, IPAddress ipAddress, int port)
        : this(playerNumber, new(ipAddress, port)) { }

    /// <summary>
    /// Player network endpoint
    /// </summary>
    public IPEndPoint EndPoint { get; } = endpoint;
}

/// <summary>
/// Holds data for a new player of type <see cref="PlayerType.Spectator"/>.
/// </summary>
/// <param name="endpoint">Player IP Endpoint <see cref="IPEndPoint"/></param>
[Serializable]
public sealed class Spectator(IPEndPoint endpoint) : Player(PlayerType.Spectator, 0)
{
    /// <summary>
    /// Initialize new <see cref="Spectator"/>
    /// </summary>
    /// <param name="ipAddress">Player IP Address</param>
    /// <param name="port">Player remote port number</param>
    public Spectator(IPAddress ipAddress, int port) : this(new(ipAddress, port)) { }

    /// <summary>
    /// Player network endpoint
    /// </summary>
    public IPEndPoint EndPoint { get; } = endpoint;
}
