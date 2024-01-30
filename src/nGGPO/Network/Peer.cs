using System.Net;

namespace nGGPO.Network;

sealed class Peer(IPEndPoint endPoint) : IEquatable<Peer>
{
    public IPEndPoint EndPoint { get; } = endPoint;
    public SocketAddress Address { get; } = endPoint.Serialize();

    public static implicit operator Peer(IPEndPoint endPoint) => new(endPoint);
    public static implicit operator IPEndPoint(Peer peer) => peer.EndPoint;
    public static implicit operator SocketAddress(Peer peer) => peer.Address;

    public bool Equals(Peer? other)
    {
        if (other is null) return false;
        return ReferenceEquals(this, other) || Address.Equals(other.Address);
    }

    public override bool Equals(object? obj) => ReferenceEquals(this, obj) || obj is Peer other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(EndPoint, Address);
    public static bool operator ==(Peer? left, Peer? right) => Equals(left, right);
    public static bool operator !=(Peer? left, Peer? right) => !Equals(left, right);
}
