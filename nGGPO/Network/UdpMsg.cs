using System.Runtime.InteropServices;
using nGGPO.Network.Messages;

namespace nGGPO.Network;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
struct UdpMsg
{
    const int HeaderSize = 5;

    [FieldOffset(0)]
    public Header Header;

    [FieldOffset(HeaderSize)]
    public SyncRequest SyncRequest;

    [FieldOffset(HeaderSize)]
    public SyncReply SyncReply;

    [FieldOffset(HeaderSize)]
    public QualityReport QualityReport;

    [FieldOffset(HeaderSize)]
    public QualityReply QualityReply;

    [FieldOffset(HeaderSize)]
    public InputAck InputAck;

    [FieldOffset(HeaderSize)]
    public InputMsg Input;

    public int PacketSize() => Marshal.SizeOf(Header) + PayloadSize();

    int PayloadSize() =>
        Header.Type switch
        {
            MsgType.SyncRequest => Marshal.SizeOf(SyncRequest),
            MsgType.SyncReply => Marshal.SizeOf(SyncReply),
            MsgType.QualityReport => Marshal.SizeOf(QualityReport),
            MsgType.QualityReply => Marshal.SizeOf(QualityReply),
            MsgType.InputAck => Marshal.SizeOf(InputAck),
            MsgType.Input => Marshal.SizeOf(Input),
            MsgType.KeepAlive => 0,
            _ => 0,
        };
}