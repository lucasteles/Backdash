using System.Runtime.InteropServices;
using Backdash.Serialization;
using Backdash.Serialization.Buffer;

namespace Backdash.Network.Messages;

[StructLayout(LayoutKind.Sequential)]
record struct SyncReply : IBinarySerializable
{
    public uint RandomReply; /* please reply back with this random data */

    public readonly void Serialize(NetworkBufferWriter writer) => writer.Write(RandomReply);

    public void Deserialize(NetworkBufferReader reader) => RandomReply = reader.ReadUInt();
}
