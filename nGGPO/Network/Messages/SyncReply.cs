using nGGPO.Serialization;
using nGGPO.Serialization.Buffer;

namespace nGGPO.Network.Messages;

struct SyncReply
{
    public uint RandomReply; /* please reply back with this random data */

    public const int Size = sizeof(uint);

    public class Serializer : BinarySerializer<SyncReply>
    {
        public static readonly Serializer Instance = new();

        protected internal override void Serialize(
            scoped NetworkBufferWriter writer,
            in SyncReply data)
        {
            writer.Write(data.RandomReply);
        }

        protected internal override SyncReply Deserialize(scoped NetworkBufferReader reader) =>
            new()
            {
                RandomReply = reader.ReadUInt(),
            };
    }
}