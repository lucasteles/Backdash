namespace Backdash.Analyzers;

class Templates
{
    public const string BaseSerializer =
        """
        #pragma warning disable CS1591
        using Backdash.Serialization;
        using Backdash.Serialization.Buffer;

        public partial class [[NAME]]: BinarySerializer<[[TYPE]]>
        {
            public static readonly IBinarySerializer<[[TYPE]]> Shared = new [[NAME]]();

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
