namespace Backdash.Analyzers;

static class Templates
{
    public const string BaseSerializer =
        """
        #pragma warning disable CS1591
        using Backdash.Serialization;
        using Backdash.Serialization.Numerics;

        public partial class [[NAME]]
        {
            public static readonly [[NAME]] Shared = new [[NAME]]();

            public void Serialize(in BinaryBufferWriter binaryWriter, in [[TYPE]] data)
            {
        [[WRITES]]
            }

            public void Deserialize(in BinaryBufferReader binaryReader, ref [[TYPE]] result)
            {
        [[READS]]
            }
        }

        """;
}
