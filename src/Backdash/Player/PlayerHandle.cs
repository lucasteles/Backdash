using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Backdash.Serialization.Internal;

namespace Backdash;

/// <summary>
///     Session player identification .
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly struct PlayerHandle : IUtf8SpanFormattable,
    IEquatable<PlayerHandle>,
    IEqualityOperators<PlayerHandle, PlayerHandle, bool>
{
    /// <summary>
    ///     Player type
    /// </summary>
    public readonly PlayerType Type;

    internal readonly sbyte QueueIndex;

    /// <summary>
    ///     Player index (starting from <c>0</c>)
    /// </summary>
    public sbyte Index => QueueIndex;

    /// <summary>
    ///     Player number (starting from <c>1</c>)
    /// </summary>
    public int Number => QueueIndex + 1;

    internal PlayerHandle(PlayerType type, int queue) : this(type, checked((sbyte)queue)) { }

    internal PlayerHandle(PlayerType type, sbyte queue = -1)
    {
        Type = type;
        QueueIndex = queue;
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

        if (!writer.Write(QueueIndex)) return false;
        if (!writer.Write("}"u8)) return false;
        return true;
    }

    /// <inheritdoc />
    public bool Equals(PlayerHandle other) =>
        Type == other.Type && QueueIndex == other.QueueIndex;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is PlayerHandle other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Type, QueueIndex);

    /// <inheritdoc />
    public static bool operator ==(PlayerHandle left, PlayerHandle right) => left.Equals(right);

    /// <inheritdoc />
    public static bool operator !=(PlayerHandle left, PlayerHandle right) => !left.Equals(right);
}
