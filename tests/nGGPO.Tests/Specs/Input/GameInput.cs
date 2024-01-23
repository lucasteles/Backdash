using nGGPO.Input;

namespace nGGPO.Tests.Specs.Input;

public class GameInputTests
{
    [Fact]
    public void InputString()
    {
        var buffer = NewBuffer(2);

        var strValue = buffer.ToString();
        const string expected =
            "00000010-00000000-00000000-00000000-00000000-00000000-00000000-00000000-00000000"
            + "|" +
            "00000000-00000000-00000000-00000000-00000000-00000000-00000000-00000000-00000000";

        strValue.Should().Be(expected);
    }


    [Fact]
    public void InputString2Players()
    {
        GameInputBuffer buffer = new();
        var p1 = GameInputBuffer.GetPlayer(ref buffer, 0);
        var p2 = GameInputBuffer.GetPlayer(ref buffer, 1);

        p1[0] = 2;
        p2[0] = 7;

        var strValue = buffer.ToString();
        const string expected =
            "00000010-00000000-00000000-00000000-00000000-00000000-00000000-00000000-00000000"
            + "|" +
            "00000111-00000000-00000000-00000000-00000000-00000000-00000000-00000000-00000000";

        strValue.Should().Be(expected);
    }

    public static GameInputBuffer NewBuffer(params byte[] bytes) => new(bytes);
}
