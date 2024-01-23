using System.Runtime.InteropServices;
using nGGPO.Network.Messages;
using nGGPO.Serialization.Buffer;

namespace nGGPO.Network;

[StructLayout(LayoutKind.Explicit)]
struct UdpMsg
{
    [FieldOffset(0)]
    public Header Header;

    [FieldOffset(Header.Size)]
    public SyncRequest SyncRequest;

    [FieldOffset(Header.Size)]
    public SyncReply SyncReply;

    [FieldOffset(Header.Size)]
    public QualityReport QualityReport;

    [FieldOffset(Header.Size)]
    public QualityReply QualityReply;

    [FieldOffset(Header.Size)]
    public InputAck InputAck;

    [FieldOffset(Header.Size)]
    public KeepAlive KeepAlive;

    [FieldOffset(Header.Size)]
    public InputMsg Input;

    public UdpMsg(MsgType type)
    {
        Header = new(type);
        SyncRequest = default;
        SyncReply = default;
        QualityReport = default;
        QualityReply = default;
        InputAck = default;
        Input = default;
    }

    public readonly void Serialize(NetworkBufferWriter writer)
    {
        Header.Serialize(writer);
        switch (Header.Type)
        {
            case MsgType.SyncRequest:
                SyncRequest.Serialize(writer);
                break;
            case MsgType.SyncReply:
                SyncReply.Serialize(writer);
                break;
            case MsgType.QualityReport:
                QualityReport.Serialize(writer);
                break;
            case MsgType.QualityReply:
                QualityReply.Serialize(writer);
                break;
            case MsgType.InputAck:
                InputAck.Serialize(writer);
                break;
            case MsgType.KeepAlive:
                break;
            case MsgType.Input:
                Input.Serialize(writer);
                break;
            case MsgType.Invalid:
                throw new InvalidOperationException();
            default:
                throw new InvalidOperationException();
        }
    }

    public void Deserialize(NetworkBufferReader reader)
    {
        Header.Deserialize(reader);
        switch (Header.Type)
        {
            case MsgType.SyncRequest:
                SyncRequest.Deserialize(reader);
                break;
            case MsgType.SyncReply:
                SyncReply.Deserialize(reader);
                break;
            case MsgType.QualityReport:
                QualityReport.Deserialize(reader);
                break;
            case MsgType.QualityReply:
                QualityReply.Deserialize(reader);
                break;
            case MsgType.InputAck:
                InputAck.Deserialize(reader);
                break;
            case MsgType.KeepAlive:
                break;
            case MsgType.Input:
                Input.Deserialize(reader);
                break;
            case MsgType.Invalid:
                throw new InvalidOperationException();
            default:
                throw new InvalidOperationException();
        }
    }
}
