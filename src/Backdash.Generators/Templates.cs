namespace Backdash.Generators;

class Templates
{
    public const string BaseSerializer =
        """
        using Backdash.Serialization;
        using Backdash.Serialization.Buffer;

        public class [[NAME]]Serializer : BinarySerializer<[[TYPE]]>
        {
            protected override void Serialize(in BinarySpanWriter binaryWriter, in [[TYPE]] data)
            {
                [[WRITES]]
            }
            protected override void Deserialize(in BinarySpanReader binaryReader, ref [[TYPE]] result)
            {
                [[READS]]
            }
        }
        """;
}
