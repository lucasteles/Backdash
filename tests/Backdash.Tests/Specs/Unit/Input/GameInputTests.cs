using Backdash.Sync;

namespace Backdash.Tests.Specs.Unit.Input;

public class GameInputTests
{
    [Fact]
    public void InputString()
    {
        GameInputBuffer buffer = new([2]);

        var strValue = buffer.ToString();
        const string expected = "00000010";

        strValue.Should().Be(expected);
    }

    [Fact]
    public void InputString2Players()
    {
        GameInputBuffer buffer = new();
        var p1 = buffer[..];

        p1[0] = 2;
        p1[1] = 7;

        var strValue = buffer.ToString();
        const string expected = "00000010-00000111";

        strValue.Should().Be(expected);
    }
}
