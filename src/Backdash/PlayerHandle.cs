using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Backdash.Core;
using Backdash.Serialization.Buffer;

namespace Backdash;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly struct PlayerHandle : IUtf8SpanFormattable,
    IEquatable<PlayerHandle>,
    IEqualityOperators<PlayerHandle, PlayerHandle, bool>
{
    public readonly int Number;
    public readonly PlayerType Type;
    internal readonly int InternalQueue;

    internal PlayerHandle(PlayerType type, int number, int queue = -1)
    {
        Number = number;
        Type = type;
        InternalQueue = queue;
    }

    public bool IsSpectator() => Type is PlayerType.Spectator;
    public bool IsRemote() => Type is PlayerType.Remote;
    public bool IsLocal() => Type is PlayerType.Local;

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

        builder.Append(Number);

        builder.Append('}');
        return builder.ToString();
    }

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

    public bool Equals(PlayerHandle other) => Number == other.Number && Type == other.Type;
    public override bool Equals(object? obj) => obj is PlayerHandle other && Equals(other);
    public override int GetHashCode() => HashCode.Combine((int)Type, Number);
    public static bool operator ==(PlayerHandle left, PlayerHandle right) => left.Equals(right);
    public static bool operator !=(PlayerHandle left, PlayerHandle right) => !left.Equals(right);

    public static PlayerHandle Local(int number)
    {
        ThrowHelpers.ThrowIfArgumentIsNegative(number);
        return new PlayerHandle(PlayerType.Local, number);
    }
}

public enum PlayerType
{
    Local,
    Remote,
    Spectator,
}
