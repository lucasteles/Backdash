using System.Runtime.InteropServices;
using nGGPO.Network.Messages;
using nGGPO.Serialization;
using nGGPO.Serialization.Buffer;

namespace nGGPO.Network;

[StructLayout(LayoutKind.Explicit)]
struct UdpMsg(MsgType type) : IBinarySerializable, IEquatable<UdpMsg>
{
    [FieldOffset(0)]
    public Header Header = new(type);

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

    public readonly bool Equals(UdpMsg other) =>
        Header.Type == other.Header.Type && Header.Type switch
        {
            MsgType.Invalid => other.Header.Type is MsgType.Invalid,
            MsgType.SyncRequest => SyncRequest.Equals(other.SyncRequest),
            MsgType.SyncReply => SyncReply.Equals(other.SyncReply),
            MsgType.Input => Input.Equals(other.Input),
            MsgType.QualityReport => QualityReport.Equals(other.QualityReport),
            MsgType.QualityReply => QualityReply.Equals(other.QualityReply),
            MsgType.KeepAlive => KeepAlive.Equals(other.KeepAlive),
            MsgType.InputAck => InputAck.Equals(other.InputAck),
            _ => throw new ArgumentOutOfRangeException(nameof(other)),
        };

    public override readonly bool Equals(object? obj) => obj is UdpMsg msg && Equals(msg);
    public override readonly int GetHashCode() => HashCode.Combine(typeof(UdpMsg));
    public static bool operator ==(UdpMsg left, UdpMsg right) => left.Equals(right);
    public static bool operator !=(UdpMsg left, UdpMsg right) => !left.Equals(right);
}
