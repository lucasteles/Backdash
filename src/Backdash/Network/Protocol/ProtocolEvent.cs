using Backdash.Serialization.Buffer;
using Backdash.Sync;

namespace Backdash.Network.Protocol;

public enum ProtocolEvent : byte
{
    Input,
    Connected,
    Synchronizing,
    Synchronized,
    Disconnected,
    NetworkInterrupted,
    NetworkResumed,
}

struct ProtocolEventInfo<TInput>(ProtocolEvent type, PlayerHandle player) : IUtf8SpanFormattable where TInput : struct
{
    public readonly ProtocolEvent Type = type;

    public PlayerHandle Player = player;

    public GameInput<TInput> Input = default;

    public SynchronizingEventInfo Synchronizing = default;

    public SynchronizedEventInfo Synchronized = default;

    public ConnectionInterruptedEventInfo NetworkInterrupted = default;

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
            case ProtocolEvent.NetworkInterrupted:
                if (!writer.Write("Timeout: "u8)) return false;
                if (!writer.Write(NetworkInterrupted.DisconnectTimeout)) return false;
                return true;
            case ProtocolEvent.Synchronizing:
                if (!writer.Write(' ')) return false;
                if (!writer.Write(Synchronizing.CurrentStep)) return false;
                if (!writer.Write('/')) return false;
                if (!writer.Write(Synchronizing.TotalSteps)) return false;
                return true;
            case ProtocolEvent.Input:
                return writer.Write(Input);
            default:
                return writer.Write("{}"u8);
        }
    }
}
