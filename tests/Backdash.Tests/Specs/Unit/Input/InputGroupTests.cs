using Backdash.Network;
using Backdash.Serialization;
using Backdash.Sync.Input.Spectator;

namespace Backdash.Tests.Specs.Unit.Input;

public class InputGroupTests
{
    [PropertyTest]
    internal void ShouldSerializeAndDeserializeGroupSamples(InputGroup<int> inputData, bool network)
    {
        IBinarySerializer<InputGroup<int>> serializer =
            new InputGroupSerializer<int>(new IntegerBinarySerializer<int>(Platform.GetEndianness(network)))
            {
                Network = network,
            };

        Span<byte> buffer = stackalloc byte[(inputData.Count * sizeof(int)) + 1];

        var writtenCount = serializer.Serialize(inputData, buffer);

        InputGroup<int> result = new();
        var readCount = serializer.Deserialize(buffer, ref result);

        readCount.Should().Be(writtenCount);
        result.Should().BeEquivalentTo(inputData);
    }
}
