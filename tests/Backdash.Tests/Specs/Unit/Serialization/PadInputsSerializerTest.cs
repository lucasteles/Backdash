using Backdash.GamePad;
using Backdash.Serialization;
using Backdash.Tests.TestUtils.Types;

namespace Backdash.Tests.Specs.Unit.Serialization;

public class PadInputsSerializerTest
{
    [Fact]
    public void ShouldSerializeAndDeserialize()
    {
        PadInputs pad = new()
        {
            Buttons = PadInputs.PadButtons.Down | PadInputs.PadButtons.X,
            LeftTrigger = 128,
            RightTrigger = 12,
            LeftAxis = new(1, 2),

            RightAxis = new(-3, -4),
        };

        IBinarySerializer<PadInputs> serializer = new PadInputsBinarySerializer();
        var buffer = new byte[1024];

        var written = serializer.Serialize(in pad, buffer);

        PadInputs restored = new();
        serializer.Deserialize(buffer.AsSpan()[..written], ref restored);

        restored.Should().BeEquivalentTo(pad);
    }
}
