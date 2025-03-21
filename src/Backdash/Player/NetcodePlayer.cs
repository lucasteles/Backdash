using System.Net;
using System.Numerics;
using Backdash.Core;

namespace Backdash;

/// <summary>
///     Holds data of a player to be added to <see cref="INetcodeSession{TInput}" />.
/// </summary>
[Serializable]
public class NetcodePlayer : IEquatable<NetcodePlayer>, IEqualityOperators<NetcodePlayer, NetcodePlayer, bool>
{
    /// <summary>
    /// Initializes a new netcode player
    /// </summary>
    public NetcodePlayer(PlayerType type, IPEndPoint? endPoint = null)
    {
        ThrowIf.InvalidEnum(type);

        if (type is not PlayerType.Local && endPoint is null)
            throw new ArgumentException($"EndPoint is required for player type: {type}", nameof(endPoint));

        Handle = new(type);
        EndPoint = endPoint;
    }

    /// <summary>
    ///     Holds data for a  player IP Endpoint
    /// </summary>
    public IPEndPoint? EndPoint { get; }

    /// <summary>
    ///     Player handler, used to identify any player in session.
    /// </summary>
    public PlayerHandle Handle { get; internal set; }

    /// <inheritdoc cref="PlayerHandle.Type" />
    public PlayerType Type => Handle.Type;

    /// <inheritdoc cref="PlayerHandle.Index" />
    public int Index => Handle.Index;

    /// <inheritdoc cref="PlayerHandle.IsSpectator()" />
    public bool IsSpectator() => Handle.IsSpectator();

    /// <inheritdoc cref="PlayerHandle.IsRemote()" />
    public bool IsRemote() => Handle.IsRemote();

    /// <inheritdoc cref="PlayerHandle.IsLocal()" />
    public bool IsLocal() => Handle.IsLocal();

    /// <inheritdoc cref="PlayerHandle.ToString()" />
    public sealed override string ToString() => Handle.ToString();

    /// <inheritdoc />
    public virtual bool Equals(NetcodePlayer? other) => Equals(this, other);

    /// <inheritdoc />
    public sealed override bool Equals(object? obj) => obj is NetcodePlayer player && Equals(player);

    /// <inheritdoc />
    public sealed override int GetHashCode() => HashCode.Combine(Type, Index);

    static bool Equals(NetcodePlayer? left, NetcodePlayer? right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left == null || right == null) return false;
        return left.Handle.Equals(right.Handle);
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
    public static NetcodePlayer CreateRemote(IPEndPoint endPoint) => new(PlayerType.Remote, endPoint);

    /// <summary>
    ///   Create new <see cref="NetcodePlayer"/> of type <see cref="PlayerType.Spectator"/>
    /// </summary>
    public static NetcodePlayer CreateSpectator(IPEndPoint endPoint) => new(PlayerType.Spectator, endPoint);
}
