using nGGPO.Serialization;
using nGGPO.Serialization.Buffer;

namespace nGGPO.Network.Messages;

struct SyncRequest
{
    public uint RandomRequest; /* please reply back with this random data */
    public ushort RemoteMagic;
    public byte RemoteEndpoint;

    public const int Size =
        sizeof(uint)
        + sizeof(ushort)
        + sizeof(byte);

    public class Serializer : BinarySerializer<SyncRequest>
    {
        public static readonly Serializer Instance = new();

        protected internal override void Serialize(
            scoped NetworkBufferWriter writer, in SyncRequest data)
        {
            writer.Write(data.RandomRequest);
            writer.Write(data.RemoteMagic);
            writer.Write(data.RemoteEndpoint);
        }

        protected internal override SyncRequest Deserialize(scoped NetworkBufferReader reader) =>
            new()
            {
                RandomRequest = reader.ReadUInt(),
                RemoteMagic = reader.ReadUShort(),
                RemoteEndpoint = reader.ReadByte(),
            };
    }
}