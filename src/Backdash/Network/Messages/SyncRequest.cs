using System.Runtime.InteropServices;
using Backdash.Serialization;
using Backdash.Serialization.Buffer;

namespace Backdash.Network.Messages;

[StructLayout(LayoutKind.Sequential)]
record struct SyncRequest : IBinarySerializable
{
    public uint RandomRequest; /* please reply back with this random data */
    public ushort RemoteMagic;

    public readonly void Serialize(NetworkBufferWriter writer)
    {
        writer.Write(in RandomRequest);
        writer.Write(in RemoteMagic);
    }

    public void Deserialize(NetworkBufferReader reader)
    {
        RandomRequest = reader.ReadUInt();
        RemoteMagic = reader.ReadUShort();
    }
}
