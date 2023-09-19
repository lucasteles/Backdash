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
struct UdpEvent
{
    public record struct SynchronizingData(int Total, int Count);

    public record struct NetworkInterruptedData(int DisconnectTimeout);

    [FieldOffset(0)]
    public UdpEventType Type = UdpEventType.Unknown;

    [FieldOffset(sizeof(sbyte))]
    public GameInput Input;

    [FieldOffset(sizeof(sbyte))]
    public SynchronizingData Synchronizing;

    [FieldOffset(sizeof(sbyte))]
    public NetworkInterruptedData NetworkInterrupted;

    public UdpEvent(UdpEventType type)
    {
        Type = type;
        Input = default;
        Synchronizing = default;
        NetworkInterrupted = default;
    }
}