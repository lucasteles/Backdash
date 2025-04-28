using Backdash.Serialization.Internal;

namespace Backdash.Network.Protocol;

enum ProtocolEvent : byte
{
    Connected,
    Synchronizing,
    Synchronized,
    SyncFailure,
    Disconnected,
    NetworkInterrupted,
    NetworkResumed,
}

struct ProtocolEventInfo(ProtocolEvent type, NetcodePlayer player) : IUtf8SpanFormattable
{
    public readonly ProtocolEvent Type = type;
    public NetcodePlayer Player = player;
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
        if (!writer.Write(Player.Index)) return false;
        if (!writer.Write(" ProtoEvt "u8)) return false;
        if (!writer.WriteEnum(Type)) return false;
        if (!writer.Write(":"u8)) return false;
        switch (Type)
        {
            case ProtocolEvent.NetworkInterrupted:
                return writer.Write("Timeout: "u8)
                       && writer.Write(NetworkInterrupted.DisconnectTimeout);
            case ProtocolEvent.Synchronizing when !writer.Write(' '):
                return false;
            case ProtocolEvent.Synchronizing:
                return writer.Write(Synchronizing.CurrentStep)
                       && writer.Write('/')
                       &&
                       writer.Write(Synchronizing.TotalSteps);
            default:
                return writer.Write("{}"u8);
        }
    }
}
