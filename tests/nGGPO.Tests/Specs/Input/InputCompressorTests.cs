using nGGPO.Data;
using nGGPO.Input;

namespace nGGPO.Tests.Specs.Input;

public class InputCompressorTests
{
    GameInput lastRecv = GameInput.Empty;

    [Fact]
    public void Test1()
    {
        InputCompressor compressor = new();
        var lastAcked = CreateInput(0, [1]);
        var lastSent = GameInput.Empty;

        var pendingInputs = CreateBuffer(
            CreateInput(1, [2], [1]),
            CreateInput(2, [4], [3]),
            CreateInput(3, [6], [7])
        );

        var compressed = compressor.Compress(
            ref lastAcked,
            in pendingInputs,
            ref lastSent
        );

        List<string> decompressedInputs = [];
        compressor.Decompress(
            ref compressed,
            ref lastRecv,
            () => decompressedInputs.Add(lastRecv.Buffer.ToString()));

        decompressedInputs.Should().BeEquivalentTo(
            "00000010-00000000-00000000-00000000-00000000-00000000-00000000-00000000-00000000"
            + "|00000001-00000000-00000000-00000000-00000000-00000000-00000000-00000000-00000000",
            "00000100-00000000-00000000-00000000-00000000-00000000-00000000-00000000-00000000"
            + "|00000011-00000000-00000000-00000000-00000000-00000000-00000000-00000000-00000000",
            "00000110-00000000-00000000-00000000-00000000-00000000-00000000-00000000-00000000"
            + "|00000111-00000000-00000000-00000000-00000000-00000000-00000000-00000000-00000000");
    }

    static GameInput CreateInput(int frame, byte[] player1, byte[]? player2 = null)
    {
        var result = new GameInput(GameInputBuffer.Capacity);
        var p1 = GameInputBuffer.ForPlayer(ref result.Buffer, 0);
        player1.CopyTo(p1);

        if (player2 is { Length: > 0 })
        {
            var p2 = GameInputBuffer.ForPlayer(ref result.Buffer, 1);
            player2.CopyTo(p2);
        }

        result.SetFrame(new(frame));
        return result;
    }

    static CircularBuffer<T> CreateBuffer<T>(params T[] values) where T : notnull
    {
        CircularBuffer<T> res = new();
        foreach (var v in values) res.Push(v);
        return res;
    }
}
