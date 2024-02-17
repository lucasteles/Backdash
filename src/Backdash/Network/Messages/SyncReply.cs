using System.Runtime.InteropServices;
using Backdash.Serialization;
using Backdash.Serialization.Buffer;

namespace Backdash.Network.Messages;

[StructLayout(LayoutKind.Sequential)]
record struct SyncReply : IBinarySerializable
{
    public uint RandomReply; /* please reply back with this random data */

    public readonly void Serialize(BinaryBufferWriter writer) => writer.Write(in RandomReply);

    public void Deserialize(BinaryBufferReader reader) => RandomReply = reader.ReadUInt();
}
