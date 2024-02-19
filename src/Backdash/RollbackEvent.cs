using System.Runtime.InteropServices;
using Backdash.Serialization.Buffer;

namespace Backdash;

public enum RollbackEvent : sbyte
{
    Unknown = -1,
    ConnectedToPeer,
    SynchronizingWithPeer,
    SynchronizedWithPeer,
    Running,
    DisconnectedFromPeer,
    TimeSync,
    ConnectionInterrupted,
    ConnectionResumed,
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public readonly record struct RollbackEventInfo : IUtf8SpanFormattable
{
    const int HeaderSize = sizeof(RollbackEvent);

    [field: FieldOffset(0)]
    public RollbackEvent Type { get; init; }

    [field: FieldOffset(HeaderSize)]
    public SynchronizingEventInfo Synchronizing { get; init; }

    [field: FieldOffset(HeaderSize)]
    public PlayerInfoEvent Connected { get; init; }

    [field: FieldOffset(HeaderSize)]
    public PlayerInfoEvent Synchronized { get; init; }

    [field: FieldOffset(HeaderSize)]
    public PlayerInfoEvent Disconnected { get; init; }

    [field: FieldOffset(HeaderSize)]
    public TimeSyncEventInfo TimeSync { get; init; }

    [field: FieldOffset(HeaderSize)]
    public ConnectionInterruptedEventInfo ConnectionInterrupted { get; init; }

    [field: FieldOffset(HeaderSize)]
    public PlayerInfoEvent ConnectionResumed { get; init; }

    public override string ToString()
    {
        var details = Type switch
        {
            RollbackEvent.Unknown or RollbackEvent.Running => "{}",
            RollbackEvent.ConnectedToPeer => Connected.ToString(),
            RollbackEvent.SynchronizingWithPeer => Synchronizing.ToString(),
            RollbackEvent.SynchronizedWithPeer => Synchronized.ToString(),
            RollbackEvent.DisconnectedFromPeer => Disconnected.ToString(),
            RollbackEvent.TimeSync => TimeSync.ToString(),
            RollbackEvent.ConnectionInterrupted => ConnectionInterrupted.ToString(),
            RollbackEvent.ConnectionResumed => ConnectionResumed.ToString(),
            _ => throw new InvalidOperationException(),
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
        if (!writer.Write("Event "u8)) return false;
        if (!writer.WriteEnum(Type)) return false;
        if (!writer.Write(':')) return false;

        switch (Type)
        {
            case RollbackEvent.Unknown or RollbackEvent.Running:
                return writer.Write("{}"u8);
            case RollbackEvent.ConnectedToPeer:
                return writer.Write(Connected.Player);
            case RollbackEvent.SynchronizingWithPeer:
                if (!writer.Write(Synchronizing.Player)) return false;
                if (!writer.Write(' ')) return false;
                if (!writer.Write(Synchronizing.CurrentStep)) return false;
                if (!writer.Write('/')) return false;
                if (!writer.Write(Synchronizing.TotalSteps)) return false;
                return true;
            case RollbackEvent.SynchronizedWithPeer:
                return writer.Write(Synchronized.Player);
            case RollbackEvent.DisconnectedFromPeer:
                return writer.Write(Disconnected.Player);
            case RollbackEvent.TimeSync:
                return writer.Write(TimeSync.FramesAhead);
            case RollbackEvent.ConnectionInterrupted:
                if (!writer.Write(ConnectionInterrupted.Player)) return false;
                if (!writer.Write(" with timeout "u8)) return false;
                if (!writer.Write(ConnectionInterrupted.DisconnectTimeout)) return false;
                return true;
            case RollbackEvent.ConnectionResumed:
                return writer.Write(ConnectionResumed.Player);
            default:
                return false;
        }
    }
}

[StructLayout(LayoutKind.Sequential)]
public readonly record struct TimeSyncEventInfo(int FramesAhead);

[StructLayout(LayoutKind.Sequential)]
public readonly record struct SynchronizingEventInfo(PlayerHandle Player, int CurrentStep, int TotalSteps);

[StructLayout(LayoutKind.Sequential)]
public readonly record struct ConnectionInterruptedEventInfo(PlayerHandle Player, int DisconnectTimeout);

[StructLayout(LayoutKind.Sequential)]
public readonly record struct PlayerInfoEvent(PlayerHandle Player);
