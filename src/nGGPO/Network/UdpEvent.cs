using System.Runtime.InteropServices;
using nGGPO.Input;

namespace nGGPO.Network;

public enum UdpEventType : sbyte
{
    Unknown = -1,
    Connected,
    Synchronizing,
    Synchronzied,
    Input,
    Disconnected,
    NetworkInterrupted,
    NetworkResumed,
};

[StructLayout(LayoutKind.Explicit)]
struct UdpEvent(UdpEventType type)
{
    public record struct SynchronizingData(int Total, int Count);

    public record struct NetworkInterruptedData(int DisconnectTimeout);

    [FieldOffset(0)]
    public UdpEventType Type = type;

    [FieldOffset(sizeof(sbyte))]
    public GameInput Input = default;

    [FieldOffset(sizeof(sbyte))]
    public SynchronizingData Synchronizing = default;

    [FieldOffset(sizeof(sbyte))]
    public NetworkInterruptedData NetworkInterrupted = default;
}
