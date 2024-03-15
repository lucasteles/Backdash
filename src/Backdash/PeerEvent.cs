using System.Runtime.InteropServices;
using Backdash.Serialization.Buffer;

namespace Backdash;

public enum PeerEvent : sbyte
{
    Connected,
    Synchronizing,
    Synchronized,
    SynchronizationFailure,
    Disconnected,
    ConnectionInterrupted,
    ConnectionResumed,
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public readonly struct PeerEventInfo(PeerEvent type) : IUtf8SpanFormattable
{
    const int HeaderSize = sizeof(PeerEvent);

    [field: FieldOffset(0)]
    public PeerEvent Type { get; } = type;

    [field: FieldOffset(HeaderSize)]
    public SynchronizingEventInfo Synchronizing { get; init; }

    [field: FieldOffset(HeaderSize)]
    public SynchronizedEventInfo Synchronized { get; init; }

    [field: FieldOffset(HeaderSize)]
    public ConnectionInterruptedEventInfo ConnectionInterrupted { get; init; }

    public override string ToString()
    {
        var details = Type switch
        {
            PeerEvent.Synchronizing => Synchronizing.ToString(),
            PeerEvent.Synchronized => Synchronized.ToString(),
            PeerEvent.ConnectionInterrupted => ConnectionInterrupted.ToString(),
            _ => "{}",
        };
        return $"Event {Type}: {details}";
    }

    public bool TryFormat(
        Span<byte> utf8Destination,
        out int bytesWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? provider
    )
    {
        bytesWritten = 0;
        Utf8StringWriter writer = new(in utf8Destination, ref bytesWritten);
        if (!writer.Write("Peer Event: "u8)) return false;
        if (!writer.WriteEnum(Type)) return false;
        if (!writer.Write(' ')) return false;
        switch (Type)
        {
            case PeerEvent.Synchronizing:
                if (!writer.Write(' ')) return false;
                if (!writer.Write(Synchronizing.CurrentStep)) return false;
                if (!writer.Write('/')) return false;
                if (!writer.Write(Synchronizing.TotalSteps)) return false;
                return true;
            case PeerEvent.Synchronized:
                if (!writer.Write(" with ping "u8)) return false;
                return writer.Write(Synchronized.Ping.TotalMilliseconds, "f2");
            case PeerEvent.ConnectionInterrupted:
                if (!writer.Write(" with timeout "u8)) return false;
                if (!writer.Write(ConnectionInterrupted.DisconnectTimeout)) return false;
                return true;
            default:
                return writer.Write(' ');
        }
    }
}

[StructLayout(LayoutKind.Sequential)]
public readonly record struct SynchronizingEventInfo(int CurrentStep, int TotalSteps);

[StructLayout(LayoutKind.Sequential)]
public readonly record struct ConnectionInterruptedEventInfo(TimeSpan DisconnectTimeout);

[StructLayout(LayoutKind.Sequential)]
public readonly record struct SynchronizedEventInfo(TimeSpan Ping);
