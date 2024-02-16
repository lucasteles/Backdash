using System.Net;
using System.Runtime.InteropServices;
using Backdash.Serialization.Buffer;

namespace Backdash;

public enum PlayerType
{
    Local,
    Remote,
    Spectator,
}

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly record struct PlayerHandle : IUtf8SpanFormattable
{
    internal const byte Size = 3 * 4;

    public readonly int Number;
    public readonly PlayerType Type;
    internal readonly int Index;

    internal PlayerHandle(PlayerType type, int number, int queue = -1)
    {
        Number = number;
        Type = type;
        Index = queue >= 0 ? queue : Math.Max(number - 1, -1);
    }

    public string ToString(string? format, IFormatProvider? formatProvider) =>
        $"{{{Type} Player {Number} at queue {Index}}}";

    public bool TryFormat(Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format,
        IFormatProvider? provider)
    {
        bytesWritten = 0;
        Utf8StringWriter writer = new(utf8Destination, ref bytesWritten);
        if (!writer.Write('{')) return false;
        if (!writer.WriteEnum(Type)) return false;
        if (!writer.Write("Player "u8)) return false;
        if (!writer.Write(Number)) return false;
        if (!writer.Write('}')) return false;

        return true;
    }
}

public abstract class Player
{
    Player(PlayerType type, int playerNumber) => Handle = new(type, playerNumber);

    public PlayerHandle Handle { get; internal set; }

    public PlayerType Type => Handle.Type;
    public int Number => Handle.Number;

    public static implicit operator PlayerHandle(Player player) => player.Handle;

    public sealed class Local(int playerNumber) : Player(PlayerType.Local, playerNumber);

    public sealed class Remote(int playerNumber, IPEndPoint endpoint) : Player(PlayerType.Remote, playerNumber)
    {
        public IPEndPoint EndPoint { get; } = endpoint;

        public Remote(int playerNumber, IPAddress ipAddress, int port)
            : this(playerNumber, new IPEndPoint(ipAddress, port)) { }
    }

    public sealed class Spectator(IPEndPoint endpoint) : Player(PlayerType.Spectator, 0)
    {
        public IPEndPoint EndPoint { get; } = endpoint;

        public Spectator(IPAddress ipAddress, int port) : this(new IPEndPoint(ipAddress, port)) { }
    }
}
