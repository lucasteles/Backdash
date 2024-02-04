using System.Runtime.InteropServices;
using nGGPO.Data;
using nGGPO.Serialization;
using nGGPO.Serialization.Buffer;

namespace nGGPO.Network.Messages;

[StructLayout(LayoutKind.Sequential)]
record struct InputAck : IBinarySerializable
{
    public Frame AckFrame;

    public readonly void Serialize(NetworkBufferWriter writer) => writer.Write(AckFrame.Number);

    public void Deserialize(NetworkBufferReader reader) =>
        AckFrame = new(reader.ReadInt());
}
