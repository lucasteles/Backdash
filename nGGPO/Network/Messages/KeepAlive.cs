using nGGPO.Serialization;
using nGGPO.Serialization.Buffer;

namespace nGGPO.Network.Messages;

readonly struct KeepAlive
{
    public const int Size = 0;
    public static readonly KeepAlive Default = new();

    public class Serializer : BinarySerializer<KeepAlive>
    {
        public static readonly Serializer Instance = new();

        protected internal override void Serialize(
            scoped NetworkBufferWriter writer,
            in KeepAlive data)
        {
        }

        protected internal override KeepAlive Deserialize(scoped NetworkBufferReader reader) =>
            Default;
    }
}