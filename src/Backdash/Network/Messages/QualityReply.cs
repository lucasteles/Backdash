using System.Runtime.InteropServices;
using Backdash.Serialization;
using Backdash.Serialization.Buffer;

namespace Backdash.Network.Messages;

[StructLayout(LayoutKind.Sequential)]
record struct QualityReply : IBinarySerializable
{
    public long Pong;

    public readonly void Serialize(BinaryBufferWriter writer) =>
        writer.Write(in Pong);

    public void Deserialize(BinaryBufferReader reader) =>
        Pong = reader.ReadLong();
}
