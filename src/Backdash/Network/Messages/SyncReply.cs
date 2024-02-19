using System.Runtime.InteropServices;
using Backdash.Serialization;
using Backdash.Serialization.Buffer;

namespace Backdash.Network.Messages;

[StructLayout(LayoutKind.Sequential)]
record struct SyncReply : IBinarySerializable
{
    public uint RandomReply; /* please reply back with this random data */
    public long Pong;

    public readonly void Serialize(BinaryBufferWriter writer)
    {
        writer.Write(in RandomReply);
        writer.Write(in Pong);
    }

    public void Deserialize(BinaryBufferReader reader)
    {
        RandomReply = reader.ReadUInt();
        Pong = reader.ReadLong();
    }
}
