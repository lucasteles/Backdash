using System.Runtime.InteropServices;
using Backdash.Input;

namespace Backdash.Network.Protocol.Events;

public enum ProtocolEvent : sbyte
{
    Unknown = -1,
    Connected,
    Synchronizing,
    Synchronized,
    Input,
    Disconnected,
    NetworkInterrupted,
    NetworkResumed,
}

[StructLayout(LayoutKind.Explicit)]
struct ProtocolEventData(ProtocolEvent name)
{
    public readonly record struct SynchronizingData(ushort Total, ushort Count);

    public readonly record struct NetworkInterruptedData(ushort DisconnectTimeout);

    const int HeaderSize = sizeof(ProtocolEvent);

    [FieldOffset(0)]
    public ProtocolEvent Name = name;

    [FieldOffset(HeaderSize)]
    public GameInput Input = default;

    [FieldOffset(HeaderSize)]
    public SynchronizingData Synchronizing = default;

    [FieldOffset(HeaderSize)]
    public NetworkInterruptedData NetworkInterrupted = default;
}
