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

        public override int SizeOf(in SyncRequest data) => Size;

        protected internal override void Serialize(
            ref NetworkBufferWriter writer, in SyncRequest data)
        {
            writer.Write(data.RandomRequest);
            writer.Write(data.RemoteMagic);
            writer.Write(data.RemoteEndpoint);
        }

        protected internal override SyncRequest Deserialize(ref NetworkBufferReader reader) =>
            new()
            {
                RandomRequest = reader.ReadUInt(),
                RemoteMagic = reader.ReadUShort(),
                RemoteEndpoint = reader.ReadByte(),
            };
    }
}