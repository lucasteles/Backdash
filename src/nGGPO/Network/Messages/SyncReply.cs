using System.Runtime.InteropServices;
using nGGPO.Serialization;
using nGGPO.Serialization.Buffer;

namespace nGGPO.Network.Messages;

[StructLayout(LayoutKind.Sequential)]
record struct SyncReply : IBinarySerializable
{
    public uint RandomReply; /* please reply back with this random data */

    public readonly void Serialize(NetworkBufferWriter writer) => writer.Write(RandomReply);

    public void Deserialize(NetworkBufferReader reader) => RandomReply = reader.ReadUInt();
}
