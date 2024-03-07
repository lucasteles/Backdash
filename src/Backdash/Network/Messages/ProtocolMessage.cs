using System.Runtime.InteropServices;
using Backdash.Serialization;
using Backdash.Serialization.Buffer;
namespace Backdash.Network.Messages;
[StructLayout(LayoutKind.Explicit, Pack = 2)]
struct ProtocolMessage(MessageType type) : IBinarySerializable, IEquatable<ProtocolMessage>, IUtf8SpanFormattable
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
    public InputMessage Input;
    public readonly void Serialize(BinarySpanWriter writer)
    {
        Header.Serialize(writer);
        switch (Header.Type)
        {
            case MessageType.SyncRequest:
                SyncRequest.Serialize(writer);
                break;
            case MessageType.SyncReply:
                SyncReply.Serialize(writer);
                break;
            case MessageType.QualityReport:
                QualityReport.Serialize(writer);
                break;
            case MessageType.QualityReply:
                QualityReply.Serialize(writer);
                break;
            case MessageType.InputAck:
                InputAck.Serialize(writer);
                break;
            case MessageType.KeepAlive:
                break;
            case MessageType.Input:
                Input.Serialize(writer);
                break;
            case MessageType.Invalid:
                throw new InvalidOperationException();
            default:
                throw new InvalidOperationException();
        }
    }
    public void Deserialize(BinarySpanReader reader)
    {
        Header.Deserialize(reader);
        switch (Header.Type)
        {
            case MessageType.SyncRequest:
                SyncRequest.Deserialize(reader);
                break;
            case MessageType.SyncReply:
                SyncReply.Deserialize(reader);
                break;
            case MessageType.QualityReport:
                QualityReport.Deserialize(reader);
                break;
            case MessageType.QualityReply:
                QualityReply.Deserialize(reader);
                break;
            case MessageType.InputAck:
                InputAck.Deserialize(reader);
                break;
            case MessageType.KeepAlive:
                break;
            case MessageType.Input:
                Input.Deserialize(reader);
                break;
            case MessageType.Invalid:
                throw new InvalidOperationException();
            default:
                throw new InvalidOperationException();
        }
    }
    public override readonly string ToString()
    {
        var info =
            Header.Type switch
            {
                MessageType.SyncRequest => SyncRequest.ToString(),
                MessageType.SyncReply => SyncReply.ToString(),
                MessageType.Input => Input.ToString(),
                MessageType.QualityReport => QualityReport.ToString(),
                MessageType.QualityReply => QualityReply.ToString(),
                MessageType.KeepAlive => KeepAlive.ToString(),
                MessageType.InputAck => InputAck.ToString(),
                MessageType.Invalid => "{}",
                _ => "unknown",
            };
        return $"Msg({Header.Type}){info}";
    }
    public readonly bool TryFormat(
        Span<byte> utf8Destination, out int bytesWritten,
        ReadOnlySpan<char> format, IFormatProvider? provider
    )
    {
        bytesWritten = 0;
        Utf8StringWriter writer = new(in utf8Destination, ref bytesWritten);
        if (!writer.Write("Msg("u8)) return false;
        if (!writer.WriteEnum(Header.Type)) return false;
        if (!writer.Write(")"u8)) return false;
        return Header.Type switch
        {
            MessageType.Input => writer.Write(Input),
            MessageType.SyncRequest => writer.Write(SyncRequest),
            MessageType.SyncReply => writer.Write(SyncReply),
            MessageType.QualityReply => writer.Write(QualityReply),
            MessageType.QualityReport => writer.Write(QualityReport),
            MessageType.InputAck => writer.Write(InputAck),
            MessageType.KeepAlive => writer.Write("{}"u8),
            MessageType.Invalid => writer.Write("{Invalid}"u8),
            _ => true,
        };
    }
    public readonly bool Equals(ProtocolMessage other) =>
        Header.Type == other.Header.Type && Header.Type switch
        {
            MessageType.Invalid => other.Header.Type is MessageType.Invalid,
            MessageType.SyncRequest => SyncRequest.Equals(other.SyncRequest),
            MessageType.SyncReply => SyncReply.Equals(other.SyncReply),
            MessageType.Input => Input.Equals(other.Input),
            MessageType.QualityReport => QualityReport.Equals(other.QualityReport),
            MessageType.QualityReply => QualityReply.Equals(other.QualityReply),
            MessageType.KeepAlive => KeepAlive.Equals(other.KeepAlive),
            MessageType.InputAck => InputAck.Equals(other.InputAck),
            _ => throw new ArgumentOutOfRangeException(nameof(other)),
        };
    public override readonly bool Equals(object? obj) => obj is ProtocolMessage msg && Equals(msg);
    public override readonly int GetHashCode() => HashCode.Combine(typeof(ProtocolMessage));
    public static bool operator ==(ProtocolMessage left, ProtocolMessage right) => left.Equals(right);
    public static bool operator !=(ProtocolMessage left, ProtocolMessage right) => !left.Equals(right);
}
