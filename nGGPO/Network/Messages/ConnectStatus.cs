using nGGPO.Serialization;
using nGGPO.Serialization.Buffer;

namespace nGGPO.Network.Messages;

struct ConnectStatus
{
    public bool Disconnected;
    public int LastFrame;

    public const int Size = sizeof(bool) + sizeof(int);

    public class Serializer : BinarySerializer<ConnectStatus>
    {
        public static readonly Serializer Instance = new();
        public override int SizeOf(in ConnectStatus data) => Size;

        protected internal override void Serialize(
            scoped ref NetworkBufferWriter writer, in ConnectStatus data)
        {
            writer.Write(data.Disconnected);
            writer.Write(data.LastFrame);
        }

        protected internal override ConnectStatus Deserialize(scoped ref NetworkBufferReader reader) =>
            new()
            {
                Disconnected = reader.ReadBool(),
                LastFrame = reader.ReadInt(),
            };
    }
}