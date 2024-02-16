using System.Runtime.InteropServices;
using Backdash.Serialization.Buffer;

namespace Backdash;

public enum RollbackEventType : sbyte
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
public readonly record struct RollbackEvent : IUtf8SpanFormattable
{
    const int HeaderSize = sizeof(RollbackEventType);

    [field: FieldOffset(0)]
    public RollbackEventType Type { get; init; }


    [field: FieldOffset(HeaderSize)]
    public SynchronizingInfo Synchronizing { get; init; }

    [field: FieldOffset(HeaderSize)]
    public PlayerEventInfo Connected { get; init; }

    [field: FieldOffset(HeaderSize)]
    public PlayerEventInfo Synchronized { get; init; }

    [field: FieldOffset(HeaderSize)]
    public PlayerEventInfo Disconnected { get; init; }

    [field: FieldOffset(HeaderSize)]
    public TimeSyncInfo TimeSync { get; init; }

    [field: FieldOffset(HeaderSize)]
    public ConnectionInterruptedInfo ConnectionInterrupted { get; init; }

    [field: FieldOffset(HeaderSize)]
    public PlayerEventInfo ConnectionResumed { get; init; }


    [StructLayout(LayoutKind.Sequential)]
    public readonly record struct SynchronizingInfo(PlayerHandle Player, int CurrentStep, int TotalSteps);

    [StructLayout(LayoutKind.Sequential)]
    public readonly record struct TimeSyncInfo(int FramesAhead);

    [StructLayout(LayoutKind.Sequential)]
    public readonly record struct ConnectionInterruptedInfo(PlayerHandle Player, int DisconnectTimeout);

    [StructLayout(LayoutKind.Sequential)]
    public readonly record struct PlayerEventInfo(PlayerHandle Player);

    public override string ToString()
    {
        var details = Type switch
        {
            RollbackEventType.Unknown or RollbackEventType.Running => "{}",
            RollbackEventType.ConnectedToPeer => Connected.ToString(),
            RollbackEventType.SynchronizingWithPeer => Synchronizing.ToString(),
            RollbackEventType.SynchronizedWithPeer => Synchronized.ToString(),
            RollbackEventType.DisconnectedFromPeer => Disconnected.ToString(),
            RollbackEventType.TimeSync => TimeSync.ToString(),
            RollbackEventType.ConnectionInterrupted => ConnectionInterrupted.ToString(),
            RollbackEventType.ConnectionResumed => ConnectionResumed.ToString(),
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
            case RollbackEventType.Unknown or RollbackEventType.Running:
                return writer.Write("{}"u8);
            case RollbackEventType.ConnectedToPeer:
                return writer.Write(Connected.Player);
            case RollbackEventType.SynchronizingWithPeer:
                if (!writer.Write(Synchronizing.Player)) return false;
                if (!writer.Write(' ')) return false;
                if (!writer.Write(Synchronizing.CurrentStep)) return false;
                if (!writer.Write('/')) return false;
                if (!writer.Write(Synchronizing.TotalSteps)) return false;
                return true;
            case RollbackEventType.SynchronizedWithPeer:
                return writer.Write(Synchronized.Player);
            case RollbackEventType.DisconnectedFromPeer:
                return writer.Write(Disconnected.Player);
            case RollbackEventType.TimeSync:
                return writer.Write(TimeSync.FramesAhead);
            case RollbackEventType.ConnectionInterrupted:
                if (!writer.Write(ConnectionInterrupted.Player)) return false;
                if (!writer.Write(" with timeout "u8)) return false;
                if (!writer.Write(ConnectionInterrupted.DisconnectTimeout)) return false;
                return true;
            case RollbackEventType.ConnectionResumed:
                return writer.Write(ConnectionResumed.Player);
            default:
                return false;
        }
    }
}
