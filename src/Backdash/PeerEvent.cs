using System.Runtime.InteropServices;
using Backdash.Network.Protocol;
using Backdash.Serialization.Internal;

namespace Backdash;

/// <summary>
/// Peer event type
/// </summary>
public enum PeerEvent : sbyte
{
    /// <summary>Handshake with the game running on the other side of the network has been completed.</summary>
    Connected,

    /// <summary>
    /// Beginning the synchronization process with the client on the other end of the networking.
    /// The <see cref="SynchronizingEventInfo.TotalSteps"/> and <see cref="SynchronizingEventInfo.CurrentStep"/> fields
    /// in the <see cref="SynchronizingEventInfo"/> struct of the <see cref="PeerEventInfo.Synchronizing"/> object
    /// indicate the progress.
    /// </summary>
    Synchronizing,

    /// <summary>
    /// The synchronization with this peer has finished.
    /// </summary>
    Synchronized,

    /// <summary>
    /// The synchronization with this peer has fail.
    /// </summary>
    SynchronizationFailure,

    /// <summary>
    /// The network connection on the other end of the network has closed.
    /// </summary>
    Disconnected,

    /// <summary>
    /// The network connection on the other end is not responding for <see cref="ProtocolOptions.DisconnectNotifyStart"/>.
    /// The <see cref="ConnectionInterruptedEventInfo.DisconnectTimeout"/> field in the <see cref="SynchronizingEventInfo"/>
    /// struct of the <see cref="PeerEventInfo.ConnectionInterrupted"/> object contains the current connection timeout
    /// which is the difference between <see cref="ProtocolOptions.DisconnectTimeout"/> and <see cref="ProtocolOptions.DisconnectNotifyStart"/>.
    /// </summary>
    ConnectionInterrupted,

    /// <summary>
    /// The network connection on the other end of the network not responding for <see cref="ProtocolOptions.DisconnectNotifyStart"/>.
    /// </summary>
    ConnectionResumed,
}

/// <summary>
/// Data structure for <see cref="PeerEventInfo"/> notifications.
/// <seealso cref="INetcodeSessionHandler.OnPeerEvent"/>
/// </summary>
/// <param name="type">Event notification type</param>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public readonly struct PeerEventInfo(PeerEvent type) : IUtf8SpanFormattable
{
    const int HeaderSize = sizeof(PeerEvent);

    /// <summary>
    /// Event type.
    /// </summary>
    [field: FieldOffset(0)]
    public PeerEvent Type { get; } = type;

    /// <summary>
    /// Data for <see cref="PeerEvent.Synchronizing"/> event.
    /// </summary>
    [field: FieldOffset(HeaderSize)]
    public SynchronizingEventInfo Synchronizing { get; init; }

    /// <summary>
    /// Data for <see cref="PeerEvent.Synchronized"/> event.
    /// </summary>
    [field: FieldOffset(HeaderSize)]
    public SynchronizedEventInfo Synchronized { get; init; }

    /// <summary>
    /// Data for <see cref="PeerEvent.ConnectionInterrupted"/> event.
    /// </summary>
    [field: FieldOffset(HeaderSize)]
    public ConnectionInterruptedEventInfo ConnectionInterrupted { get; init; }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

/// <summary>
/// Data for <see cref="PeerEvent.Synchronizing"/> event.
/// </summary>
/// <param name="CurrentStep">Current synchronizing step.</param>
/// <param name="TotalSteps">Total synchronization steps</param>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct SynchronizingEventInfo(int CurrentStep, int TotalSteps);

/// <summary>
/// Data for <see cref="PeerEvent.ConnectionInterrupted"/> event.
/// </summary>
/// <param name="DisconnectTimeout">Time to disconnect.</param>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct ConnectionInterruptedEventInfo(TimeSpan DisconnectTimeout);

/// <summary>
/// Data for <see cref="PeerEvent.Synchronized"/> event.
/// </summary>
/// <param name="Ping">Current ping</param>
[StructLayout(LayoutKind.Sequential)]
public readonly record struct SynchronizedEventInfo(TimeSpan Ping);
