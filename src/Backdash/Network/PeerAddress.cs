using System.Net;

namespace Backdash.Network;

sealed class PeerAddress(IPEndPoint endPoint) : IEquatable<PeerAddress>
{
    public IPEndPoint EndPoint { get; } = endPoint;
    public SocketAddress Address { get; } = endPoint.Serialize();

    public PeerAddress Clone() => new(EndPoint);

    public override bool Equals(object? obj) =>
        ReferenceEquals(this, obj) || (obj is PeerAddress other && Equals(other));

    public bool Equals(PeerAddress? other)
    {
        if (other is null) return false;
        return ReferenceEquals(this, other) || Address.Equals(other.Address);
    }

    public override int GetHashCode() => HashCode.Combine(EndPoint, Address);

    public static implicit operator PeerAddress(IPEndPoint endPoint) => new(endPoint);
    public static implicit operator IPEndPoint(PeerAddress peerAddress) => peerAddress.EndPoint;
    public static implicit operator SocketAddress(PeerAddress peerAddress) => peerAddress.Address;

    public static bool operator ==(PeerAddress? left, PeerAddress? right) => Equals(left, right);
    public static bool operator !=(PeerAddress? left, PeerAddress? right) => !Equals(left, right);
}
