using FluentAssertions;
using nGGPO.Input;
using nGGPO.Utils;

namespace nGGPO.Tests;

public class GameInputTests
{
    [Fact]
    public void InputString()
    {
        var buffer = NewBuffer(2);

        var strValue = buffer.ToString();
        var expected = MountBin("10");

        strValue.Should().Be(expected);
    }

    public static GameInputBuffer NewBuffer(params byte[] bytes) => new(bytes);

    public static string ZeroBlock = new('0', Max.InputBytes * Mem.ByteSize);

    public static string MountBin(params string[] block)
    {
        block.Should().HaveCountLessOrEqualTo(Max.InputPlayers);
        var padding = Enumerable.Repeat(ZeroBlock, Max.InputPlayers - block.Length);
        var segments = padding.Concat(block
            .Select(n => n.PadLeft(Max.InputBytes * Mem.ByteSize, '0')));
        return string.Join('-', segments);
    }
}
