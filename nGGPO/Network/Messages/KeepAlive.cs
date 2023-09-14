using nGGPO.Serialization;

namespace nGGPO.Network.Messages;

readonly struct KeepAlive
{
    public const int Size = 0;
    public static readonly KeepAlive Default = new();

    public class Serializer : BinarySerializer<KeepAlive>
    {
        public static readonly Serializer Instance = new();

        public override int SizeOf(in KeepAlive data) => Size;

        protected internal override void Serialize(
            ref NetworkBufferWriter writer,
            in KeepAlive data)
        {
        }

        protected internal override KeepAlive Deserialize(ref NetworkBufferReader reader) =>
            Default;
    }
}