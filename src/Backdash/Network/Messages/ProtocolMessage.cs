using System.Runtime.InteropServices;
using Backdash.Core;
using Backdash.Serialization;
using Backdash.Serialization.Buffer;

namespace Backdash.Network.Messages;

[Serializable, StructLayout(LayoutKind.Explicit, Pack = 2)]
struct ProtocolMessage(MessageType type) : ISpanSerializable, IEquatable<ProtocolMessage>, IUtf8SpanFormattable
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
    public ConsistencyCheckRequest ConsistencyCheckRequest;

    [FieldOffset(Header.Size)]
    public ConsistencyCheckReply ConsistencyCheckReply;

    [FieldOffset(Header.Size)]
    public InputMessage Input;

    public readonly void Serialize(in BinaryRawBufferWriter writer)
    {
        Header.Serialize(in writer);
        switch (Header.Type)
        {
            case MessageType.SyncRequest:
                SyncRequest.Serialize(in writer);
                break;
            case MessageType.SyncReply:
                SyncReply.Serialize(in writer);
                break;
            case MessageType.QualityReport:
                QualityReport.Serialize(in writer);
                break;
            case MessageType.QualityReply:
                QualityReply.Serialize(in writer);
                break;
            case MessageType.InputAck:
                InputAck.Serialize(in writer);
                break;
            case MessageType.KeepAlive:
                break;
            case MessageType.ConsistencyCheckRequest:
                ConsistencyCheckRequest.Serialize(in writer);
                break;
            case MessageType.ConsistencyCheckReply:
                ConsistencyCheckReply.Serialize(in writer);
                break;
            case MessageType.Input:
                Input.Serialize(in writer);
                break;
            case MessageType.Unknown:
            default:
                throw new NetcodeSerializationException<ProtocolMessage>("Unknown message type");
        }
    }

    public void Deserialize(in BinaryBufferReader reader)
    {
        Header.Deserialize(in reader);
        switch (Header.Type)
        {
            case MessageType.SyncRequest:
                SyncRequest.Deserialize(in reader);
                break;
            case MessageType.SyncReply:
                SyncReply.Deserialize(in reader);
                break;
            case MessageType.QualityReport:
                QualityReport.Deserialize(in reader);
                break;
            case MessageType.QualityReply:
                QualityReply.Deserialize(in reader);
                break;
            case MessageType.InputAck:
                InputAck.Deserialize(in reader);
                break;
            case MessageType.KeepAlive:
                break;
            case MessageType.ConsistencyCheckRequest:
                ConsistencyCheckRequest.Deserialize(in reader);
                break;
            case MessageType.ConsistencyCheckReply:
                ConsistencyCheckReply.Deserialize(in reader);
                break;
            case MessageType.Input:
                Input.Deserialize(in reader);
                break;
            case MessageType.Unknown:
            default:
                break;
        }
    }

    public override readonly string ToString()
    {
        var info =
            Header.Type switch
            {
                MessageType.SyncRequest => SyncRequest.ToString(),
                MessageType.SyncReply => SyncReply.ToString(),
                MessageType.ConsistencyCheckRequest => ConsistencyCheckRequest.ToString(),
                MessageType.ConsistencyCheckReply => ConsistencyCheckReply.ToString(),
                MessageType.Input => Input.ToString(),
                MessageType.QualityReport => QualityReport.ToString(),
                MessageType.QualityReply => QualityReply.ToString(),
                MessageType.KeepAlive => KeepAlive.ToString(),
                MessageType.InputAck => InputAck.ToString(),
                MessageType.Unknown => "{}",
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
            MessageType.ConsistencyCheckRequest => writer.Write(ConsistencyCheckRequest),
            MessageType.ConsistencyCheckReply => writer.Write(ConsistencyCheckReply),
            MessageType.QualityReply => writer.Write(QualityReply),
            MessageType.QualityReport => writer.Write(QualityReport),
            MessageType.InputAck => writer.Write(InputAck),
            MessageType.KeepAlive => writer.Write("{}"u8),
            MessageType.Unknown => writer.Write("{Invalid}"u8),
            _ => true,
        };
    }

    public readonly bool Equals(ProtocolMessage other) =>
        Header.Type == other.Header.Type && Header.Type switch
        {
            MessageType.Unknown => other.Header.Type is MessageType.Unknown,
            MessageType.SyncRequest => SyncRequest.Equals(other.SyncRequest),
            MessageType.SyncReply => SyncReply.Equals(other.SyncReply),
            MessageType.ConsistencyCheckRequest => ConsistencyCheckRequest.Equals(other.ConsistencyCheckRequest),
            MessageType.ConsistencyCheckReply => ConsistencyCheckReply.Equals(other.ConsistencyCheckReply),
            MessageType.Input => Input.Equals(other.Input),
            MessageType.QualityReport => QualityReport.Equals(other.QualityReport),
            MessageType.QualityReply => QualityReply.Equals(other.QualityReply),
            MessageType.KeepAlive => KeepAlive.Equals(other.KeepAlive),
            MessageType.InputAck => InputAck.Equals(other.InputAck),
            _ => throw new ArgumentOutOfRangeException(nameof(other)),
        };

    public override readonly bool Equals(object? obj) => obj is ProtocolMessage msg && Equals(msg);
    public override readonly int GetHashCode() => HashCode.Combine(typeof(ProtocolMessage));
    public static bool operator ==(in ProtocolMessage left, in ProtocolMessage right) => left.Equals(right);
    public static bool operator !=(in ProtocolMessage left, in ProtocolMessage right) => !left.Equals(right);
}
