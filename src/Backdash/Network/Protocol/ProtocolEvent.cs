using System.Runtime.InteropServices;
using Backdash.Serialization.Buffer;
using Backdash.Sync;

namespace Backdash.Network.Protocol;

public enum ProtocolEventType : byte
{
    Input,
    Connected,
    Synchronizing,
    Synchronized,
    Disconnected,
    NetworkInterrupted,
    NetworkResumed,
}

[StructLayout(LayoutKind.Explicit)]
struct ProtocolEvent(ProtocolEventType type, PlayerHandle player) : IUtf8SpanFormattable
{
    public readonly record struct SynchronizingData(ushort Total, ushort Count);

    public readonly record struct NetworkInterruptedData(ushort DisconnectTimeout);

    const int HeaderSize = sizeof(ProtocolEventType) + PlayerHandle.Size;

    [FieldOffset(0)]
    public ProtocolEventType Type = type;

    [FieldOffset(1)]
    public PlayerHandle Player = player;

    [FieldOffset(HeaderSize)]
    public GameInput Input = default;

    [FieldOffset(HeaderSize)]
    public SynchronizingData Synchronizing = default;

    [FieldOffset(HeaderSize)]
    public NetworkInterruptedData NetworkInterrupted = default;

    public readonly bool TryFormat(
        Span<byte> utf8Destination, out int bytesWritten, ReadOnlySpan<char> format,
        IFormatProvider? provider)
    {
        bytesWritten = 0;
        Utf8StringWriter writer = new(in utf8Destination, ref bytesWritten);
        if (!writer.Write("P"u8)) return false;
        if (!writer.Write(Player.Number)) return false;
        if (!writer.Write(" ProtoEvt "u8)) return false;
        if (!writer.WriteEnum(Type)) return false;
        if (!writer.Write(":"u8)) return false;

        switch (Type)
        {
            case ProtocolEventType.NetworkInterrupted:
                if (!writer.Write("Timeout: "u8)) return false;
                if (!writer.Write(NetworkInterrupted.DisconnectTimeout)) return false;
                return true;
            case ProtocolEventType.Synchronizing:
                if (!writer.Write(' ')) return false;
                if (!writer.Write(Synchronizing.Count)) return false;
                if (!writer.Write('/')) return false;
                if (!writer.Write(Synchronizing.Total)) return false;
                return true;
            case ProtocolEventType.Input:
                return writer.Write(Input);
            default:
                return writer.Write("{}"u8);
        }
    }
}
