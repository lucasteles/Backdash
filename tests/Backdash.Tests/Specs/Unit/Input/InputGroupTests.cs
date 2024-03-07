using Backdash.Network;
using Backdash.Serialization;
using Backdash.Sync.Input.Spectator;
namespace Backdash.Tests.Specs.Unit.Input;
public class CombinedInputsTests
{
    [PropertyTest]
    internal void ShouldSerializeAndDeserializeGroupSamples(CombinedInputs<int> inputData, bool network)
    {
        IBinarySerializer<CombinedInputs<int>> serializer =
            new CombinedInputsSerializer<int>(new IntegerBinarySerializer<int>(Platform.GetEndianness(network)))
            {
                Network = network,
            };
        Span<byte> buffer = stackalloc byte[(inputData.Count * sizeof(int)) + 1];
        var writtenCount = serializer.Serialize(inputData, buffer);
        CombinedInputs<int> result = new();
        var readCount = serializer.Deserialize(buffer, ref result);
        readCount.Should().Be(writtenCount);
        result.Should().BeEquivalentTo(inputData);
    }
}
