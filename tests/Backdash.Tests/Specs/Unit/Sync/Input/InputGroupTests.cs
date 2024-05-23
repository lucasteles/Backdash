using Backdash.Network;
using Backdash.Serialization;
using Backdash.Synchronizing.Input.Confirmed;
using Backdash.Tests.TestUtils;

namespace Backdash.Tests.Specs.Unit.Sync.Input;

public class ConfirmedInputsTests
{
    [PropertyTest]
    internal void ShouldSerializeAndDeserializeGroupSamples(ConfirmedInputs<int> inputData, bool network)
    {
        IBinarySerializer<ConfirmedInputs<int>> serializer =
            new ConfirmedInputsSerializer<int>(new IntegerBinarySerializer<int>(Platform.GetEndianness(network)))
            {
                Network = network,
            };
        Span<byte> buffer = stackalloc byte[(inputData.Count * sizeof(int)) + 1];
        var writtenCount = serializer.Serialize(inputData, buffer);
        ConfirmedInputs<int> result = new();
        var readCount = serializer.Deserialize(buffer, ref result);
        readCount.Should().Be(writtenCount);
        result.Should().BeEquivalentTo(inputData);
    }
}
