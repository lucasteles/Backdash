using System.Runtime.InteropServices;

namespace nGGPO.Network;

public enum MsgType : byte
{
    Invalid,
    SyncRequest,
    SyncReply,
    Input,
    QualityReport,
    QualityReply,
    KeepAlive,
    InputAck,
};

[StructLayout(LayoutKind.Sequential)]
public struct Hrd
{
    public ushort Magic;
    public ushort SequenceNumber;
    public MsgType Type;
}

public class UdpConnectStatus
{
    public int LastFrame { get; set; }
}

[StructLayout(LayoutKind.Sequential)]
public struct UdpMsg
{
    public Hrd Hrd;
}