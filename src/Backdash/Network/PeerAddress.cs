using System.Net;

namespace Backdash.Network;

sealed class PeerAddress(EndPoint endPoint) : IEquatable<PeerAddress>
{
    public EndPoint EndPoint { get; } = endPoint;
    public SocketAddress Address { get; } = endPoint.Serialize();

    public override bool Equals(object? obj) =>
        ReferenceEquals(this, obj) || (obj is PeerAddress other && Equals(other));

    public bool Equals(PeerAddress? other)
    {
        if (other is null) return false;
        return ReferenceEquals(this, other) || Address.Equals(other.Address);
    }

    public override int GetHashCode() => HashCode.Combine(EndPoint, Address);

    public static implicit operator PeerAddress(EndPoint endPoint) => new(endPoint);
    public static implicit operator EndPoint(PeerAddress peerAddress) => peerAddress.EndPoint;
    public static implicit operator SocketAddress(PeerAddress peerAddress) => peerAddress.Address;

    public static bool operator ==(PeerAddress? left, PeerAddress? right) => Equals(left, right);
    public static bool operator !=(PeerAddress? left, PeerAddress? right) => !Equals(left, right);
}
