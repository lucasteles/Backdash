using System.Runtime.InteropServices;
using nGGPO.Network.Messages;

namespace nGGPO.Network;

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct UdpMsg
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
}