using System.Net;
using System.Numerics;
using System.Text;
using Backdash.Core;
using Backdash.Serialization.Internal;

namespace Backdash;

/// <summary>
///     Holds data of a player to be added to <see cref="INetcodeSession{TInput}" />.
/// </summary>
[Serializable]
public class NetcodePlayer :
    IUtf8SpanFormattable,
    IEquatable<NetcodePlayer>,
    IEqualityOperators<NetcodePlayer, NetcodePlayer, bool>

{
    sbyte queueIndex;

    /// <summary>
    ///     Player unique ID
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    ///     Player type
    /// </summary>
    public readonly PlayerType Type;

    /// <summary>
    ///     Custom user id value
    /// </summary>
    public int CustomId { get; set; }

    /// <summary>
    ///     Network stats for the peer
    /// </summary>
    /// <seealso cref="INetcodeSession.UpdateNetworkStats"/>
    public PeerNetworkStats NetworkStats = new();

    internal NetcodePlayer(sbyte queueIndex, PlayerType type, EndPoint? endPoint = null)
    {
        ThrowIf.InvalidEnum(type);

        Type = type;
        EndPoint = endPoint;
        this.queueIndex = queueIndex;
    }

    /// <summary>
    /// Initializes a new netcode player
    /// </summary>
    public NetcodePlayer(PlayerType type, EndPoint? endPoint = null) : this(-1, type, endPoint) { }

    /// <summary>
    /// Initializes a new netcode player
    /// </summary>
    public NetcodePlayer() : this(-1, PlayerType.Local) { }

    /// <summary>
    ///     Holds data for a  player IP Endpoint
    /// </summary>
    public EndPoint? EndPoint { get; }

    /// <summary>
    ///     Player number (starting from <c>1</c>)
    /// </summary>
    public int Number => queueIndex + 1;

    /// <inheritdoc cref="NetcodePlayer.Index" />
    public int Index => queueIndex;

    internal void SetQueue(sbyte value) => queueIndex = value;
    internal void SetQueue(int value) => SetQueue((sbyte)value);

    bool IUtf8SpanFormattable.TryFormat(
        Span<byte> utf8Destination, out int bytesWritten,
        ReadOnlySpan<char> format, IFormatProvider? provider)
    {
        bytesWritten = 0;
        Utf8StringWriter writer = new(utf8Destination, ref bytesWritten);
        if (!writer.Write("{"u8)) return false;
        if (IsSpectator())
        {
            if (!writer.Write("Spectator: "u8)) return false;
        }
        else
        {
            if (!writer.WriteEnum(Type)) return false;
            if (!writer.Write("Player: "u8)) return false;
        }

        if (!writer.Write(Number)) return false;
        if (!writer.Write("}"u8)) return false;
        return true;
    }

    /// <summary>
    ///     Returns <see langword="true" /> if player is <see cref="PlayerType.Spectator" />
    /// </summary>
    public bool IsSpectator() => Type is PlayerType.Spectator;

    /// <summary>
    ///     Returns <see langword="true" /> if player is <see cref="PlayerType.Remote" />
    /// </summary>
    public bool IsRemote() => Type is PlayerType.Remote;

    /// <summary>
    ///     Returns <see langword="true" /> if player is <see cref="PlayerType.Local" />
    /// </summary>
    public bool IsLocal() => Type is PlayerType.Local;

    /// <inheritdoc />
    public override string ToString()
    {
        StringBuilder builder = new();
        builder.Append('{');
        if (IsSpectator())
            builder.Append("Spectator ");
        else
        {
            builder.Append(Type);
            builder.Append("Player");
        }

        builder.Append(Index);
        builder.Append('}');
        return builder.ToString();
    }

    /// <inheritdoc />
    public virtual bool Equals(NetcodePlayer? other) => Equals(this, other);

    /// <inheritdoc />
    public sealed override bool Equals(object? obj) => obj is NetcodePlayer player && Equals(player);

    /// <inheritdoc />
    public sealed override int GetHashCode() => HashCode.Combine(Type, Index);

    static bool Equals(NetcodePlayer? left, NetcodePlayer? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left is null || right is null) return false;
        return left.Type == right.Type && left.queueIndex == right.queueIndex;
    }

    /// <inheritdoc />
    public static bool operator ==(NetcodePlayer? left, NetcodePlayer? right) => Equals(left, right);

    /// <inheritdoc />
    public static bool operator !=(NetcodePlayer? left, NetcodePlayer? right) => !Equals(left, right);

    /// <summary>
    ///   Create new <see cref="NetcodePlayer"/> of type <see cref="PlayerType.Local"/>
    /// </summary>
    public static NetcodePlayer CreateLocal() => new(PlayerType.Local);

    /// <summary>
    ///   Create new <see cref="NetcodePlayer"/> of type <see cref="PlayerType.Remote"/>
    /// </summary>
    public static NetcodePlayer CreateRemote(EndPoint endPoint) => new(PlayerType.Remote, endPoint);

    /// <summary>
    ///   Create new <see cref="NetcodePlayer"/> of type <see cref="PlayerType.Remote"/>
    /// </summary>
    public static NetcodePlayer CreateRemote(IPAddress address, int port) =>
        CreateRemote(new IPEndPoint(address, port));

    /// <summary>
    ///   Create new localhost <see cref="NetcodePlayer"/> of type <see cref="PlayerType.Remote"/>
    /// </summary>
    public static NetcodePlayer CreateRemote(int port) =>
        CreateRemote(IPAddress.Loopback, port);

    /// <summary>
    ///   Create new <see cref="NetcodePlayer"/> of type <see cref="PlayerType.Spectator"/>
    /// </summary>
    public static NetcodePlayer CreateSpectator(EndPoint endPoint) => new(PlayerType.Spectator, endPoint);

    /// <summary>
    ///   Create new <see cref="NetcodePlayer"/> of type <see cref="PlayerType.Spectator"/>
    /// </summary>
    public static NetcodePlayer CreateSpectator(IPAddress address, int port) =>
        CreateSpectator(new IPEndPoint(address, port));

    /// <summary>
    ///   Create new localhost <see cref="NetcodePlayer"/> of type <see cref="PlayerType.Spectator"/>
    /// </summary>
    public static NetcodePlayer CreateSpectator(int port) =>
        CreateSpectator(IPAddress.Loopback, port);
}
