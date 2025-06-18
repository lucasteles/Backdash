using System.Runtime.InteropServices;
using Backdash.Serialization;
using Backdash.Serialization.Internal;

namespace Backdash.Network.Messages;

[Serializable, StructLayout(LayoutKind.Sequential, Pack = 4)]
record struct SyncRequest : IUtf8SpanFormattable
{
    public ushort RandomRequest;
    public long Ping;

    public readonly void Serialize(in BinaryRawBufferWriter writer)
    {
        writer.Write(in RandomRequest);
        writer.Write(in Ping);
    }

    public void Deserialize(in BinaryBufferReader reader)
    {
        RandomRequest = reader.ReadUInt16();
        Ping = reader.ReadInt64();
    }

    public readonly bool TryFormat(
        Span<byte> utf8Destination,
        out int bytesWritten, ReadOnlySpan<char> format,
        IFormatProvider? provider
    )
    {
        bytesWritten = 0;
        using Utf8ObjectWriter writer = new(in utf8Destination, ref bytesWritten);
        return writer.Write(in RandomRequest) && writer.Write(in Ping);
    }
}
