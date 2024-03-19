using Backdash.GamePad;
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

        PadInputsBinarySerializer serializer = new();
        var buffer = new byte[1024];
        
    }
}
