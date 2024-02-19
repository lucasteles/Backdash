using System.Runtime.InteropServices;
using Backdash.Serialization;
using Backdash.Serialization.Buffer;

namespace Backdash.Network.Messages;

[StructLayout(LayoutKind.Sequential)]
record struct SyncRequest : IBinarySerializable
{
    public uint RandomRequest; /* please reply back with this random data */
    public ushort RemoteMagic;
    public long Ping;

    public readonly void Serialize(BinaryBufferWriter writer)
    {
        writer.Write(in RandomRequest);
        writer.Write(in RemoteMagic);
        writer.Write(in Ping);
    }

    public void Deserialize(BinaryBufferReader reader)
    {
        RandomRequest = reader.ReadUInt();
        RemoteMagic = reader.ReadUShort();
        Ping = reader.ReadLong();
    }
}
