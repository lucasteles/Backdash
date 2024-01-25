using System.Runtime.InteropServices;
using nGGPO.Input;

namespace nGGPO.Network.Protocol;

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
    public readonly record struct SynchronizingData(int Total, int Count);

    public readonly record struct NetworkInterruptedData(int DisconnectTimeout);

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
