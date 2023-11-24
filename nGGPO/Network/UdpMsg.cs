using System;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using nGGPO.Network.Messages;
using nGGPO.Serialization;
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

    [Pure]
    public int PacketSize() =>
        Header.Size +
        Header.Type switch
        {
            MsgType.SyncRequest => SyncRequest.Size,
            MsgType.SyncReply => SyncReply.Size,
            MsgType.QualityReport => QualityReport.Size,
            MsgType.QualityReply => QualityReply.Size,
            MsgType.InputAck => InputAck.Size,
            MsgType.KeepAlive => KeepAlive.Size,
            MsgType.Input => Input.PacketSize(),
            MsgType.Invalid => throw new InvalidOperationException(),
            _ => throw new ArgumentOutOfRangeException(),
        };
}

class UdpMsgBinarySerializer : BinarySerializer<UdpMsg>
{
    public static readonly UdpMsgBinarySerializer Instance = new();

    protected internal override void Serialize(scoped NetworkBufferWriter writer,
        scoped in UdpMsg data)
    {
        Header.Serializer.Instance.Serialize(writer, in data.Header);
        switch (data.Header.Type)
        {
            case MsgType.SyncRequest:
                SyncRequest.Serializer.Instance.Serialize(writer, in data.SyncRequest);
                break;
            case MsgType.SyncReply:
                SyncReply.Serializer.Instance.Serialize(writer, in data.SyncReply);
                break;
            case MsgType.QualityReport:
                QualityReport.Serializer.Instance.Serialize(writer, in data.QualityReport);
                break;
            case MsgType.QualityReply:
                QualityReply.Serializer.Instance.Serialize(writer, in data.QualityReply);
                break;
            case MsgType.InputAck:
                InputAck.Serializer.Instance.Serialize(writer, in data.InputAck);
                break;
            case MsgType.KeepAlive:
                KeepAlive.Serializer.Instance.Serialize(writer, in data.KeepAlive);
                break;
            case MsgType.Input:
                InputMsg.Serializer.Instance.Serialize(writer, in data.Input);
                break;
            case MsgType.Invalid:
                throw new InvalidOperationException();
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    protected internal override UdpMsg Deserialize(scoped NetworkBufferReader reader)
    {
        UdpMsg data = new()
        {
            Header = Header.Serializer.Instance.Deserialize(reader),
        };

        switch (data.Header.Type)
        {
            case MsgType.SyncRequest:
                data.SyncRequest = SyncRequest.Serializer.Instance.Deserialize(reader);
                break;
            case MsgType.SyncReply:
                data.SyncReply = SyncReply.Serializer.Instance.Deserialize(reader);
                break;
            case MsgType.QualityReport:
                data.QualityReport = QualityReport.Serializer.Instance.Deserialize(reader);
                break;
            case MsgType.QualityReply:
                data.QualityReply = QualityReply.Serializer.Instance.Deserialize(reader);
                break;
            case MsgType.InputAck:
                data.InputAck = InputAck.Serializer.Instance.Deserialize(reader);
                break;
            case MsgType.KeepAlive:
                data.KeepAlive = KeepAlive.Serializer.Instance.Deserialize(reader);
                break;
            case MsgType.Input:
                data.Input = InputMsg.Serializer.Instance.Deserialize(reader);
                break;
            case MsgType.Invalid:
                throw new InvalidOperationException();
            default:
                throw new ArgumentOutOfRangeException();
        }

        return data;
    }
}