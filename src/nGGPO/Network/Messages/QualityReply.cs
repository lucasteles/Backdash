using System.Runtime.InteropServices;
using nGGPO.Serialization;
using nGGPO.Serialization.Buffer;

namespace nGGPO.Network.Messages;

[StructLayout(LayoutKind.Sequential)]
record struct QualityReply : IBinarySerializable
{
    public uint Pong;

    public readonly void Serialize(NetworkBufferWriter writer) =>
        writer.Write(Pong);

    public void Deserialize(NetworkBufferReader reader) =>
        Pong = reader.ReadUInt();
}
