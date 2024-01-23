using System.Runtime.InteropServices;
using nGGPO.Serialization;
using nGGPO.Serialization.Buffer;

namespace nGGPO.Network.Messages;

[StructLayout(LayoutKind.Sequential)]
record struct SyncRequest : IBinarySerializable
{
    public uint RandomRequest; /* please reply back with this random data */
    public ushort RemoteMagic;
    public byte RemoteEndpoint;

    public readonly void Serialize(NetworkBufferWriter writer)
    {
        writer.Write(RandomRequest);
        writer.Write(RemoteMagic);
        writer.Write(RemoteEndpoint);
    }

    public void Deserialize(NetworkBufferReader reader)
    {
        RandomRequest = reader.ReadUInt();
        RemoteMagic = reader.ReadUShort();
        RemoteEndpoint = reader.ReadByte();
    }
}
