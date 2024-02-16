using System.Runtime.InteropServices;
using Backdash.Serialization;
using Backdash.Serialization.Buffer;

namespace Backdash.Network.Messages;

[StructLayout(LayoutKind.Explicit, Pack = 2)]
struct ProtocolMessage(MsgType type) : IBinarySerializable, IEquatable<ProtocolMessage>, IUtf8SpanFormattable
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

    public override readonly string ToString()
    {
        var info =
            Header.Type switch
            {
                MsgType.SyncRequest => SyncRequest.ToString(),
                MsgType.SyncReply => SyncReply.ToString(),
                MsgType.Input => Input.ToString(),
                MsgType.QualityReport => QualityReport.ToString(),
                MsgType.QualityReply => QualityReply.ToString(),
                MsgType.KeepAlive => KeepAlive.ToString(),
                MsgType.InputAck => InputAck.ToString(),
                MsgType.Invalid => "{}",
                _ => "unknown",
            };
        return $"ProtocolMessage({Header.Type}) = {info}";
    }

    public readonly string ToString(string? format, IFormatProvider? formatProvider) => ToString();

    public readonly bool TryFormat(
        Span<byte> utf8Destination, out int bytesWritten,
        ReadOnlySpan<char> format, IFormatProvider? provider
    )
    {
        bytesWritten = 0;
        Utf8StringWriter writer = new(in utf8Destination, ref bytesWritten);
        writer.WriteEnum(Header.Type);

        if (!writer.Write("Msg("u8)) return false;
        if (!writer.WriteEnum(Header.Type)) return false;
        if (!writer.Write(")"u8)) return false;

        return true;
    }

    public readonly bool Equals(ProtocolMessage other) =>
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

    public override readonly bool Equals(object? obj) => obj is ProtocolMessage msg && Equals(msg);
    public override readonly int GetHashCode() => HashCode.Combine(typeof(ProtocolMessage));
    public static bool operator ==(ProtocolMessage left, ProtocolMessage right) => left.Equals(right);
    public static bool operator !=(ProtocolMessage left, ProtocolMessage right) => !left.Equals(right);
}
