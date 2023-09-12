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

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Hrd
{
    public ushort Magic;
    public ushort SequenceNumber;
    public MsgType Type;
}

public struct UdpConnectStatus
{
    public int LastFrame;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct UdpMsg
{
    public Hrd Hrd;
}