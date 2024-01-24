using System.Runtime.InteropServices;
using nGGPO.Input;

namespace nGGPO.Network.Protocol;

public enum ProtocolEventName : sbyte
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
struct ProtocolEvent(ProtocolEventName name)
{
    public readonly record struct SynchronizingData(int Total, int Count);

    public readonly record struct NetworkInterruptedData(int DisconnectTimeout);

    [FieldOffset(0)]
    public ProtocolEventName Name = name;

    [FieldOffset(sizeof(sbyte))]
    public GameInput Input = default;

    [FieldOffset(sizeof(sbyte))]
    public SynchronizingData Synchronizing = default;

    [FieldOffset(sizeof(sbyte))]
    public NetworkInterruptedData NetworkInterrupted = default;
}
