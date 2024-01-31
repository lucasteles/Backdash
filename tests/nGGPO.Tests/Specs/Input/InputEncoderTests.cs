using nGGPO.Data;
using nGGPO.Input;
using nGGPO.Network.Messages;
using nGGPO.Network.Protocol.Internal;

namespace nGGPO.Tests.Specs.Input;

public class InputEncoderTests
{
    [Fact]
    public void ShouldCompressAndDecompressSample()
    {
        var lastAcked = CreateInput(0, [1]);

        GameInput[] inputList =
        [
            CreateInput(1, [2], [1]),
            CreateInput(2, [4], [3]),
            CreateInput(3, [6], [7])
        ];

        var compressed = GetCompressedMsg(in lastAcked, inputList);

        var decompressedInputs = DecompressToList(compressed, lastAcked);

        decompressedInputs.Should().BeEquivalentTo(inputList);
        decompressedInputs
            .Select(x => x.Buffer.ToString())
            .Should().BeEquivalentTo(
                "00000010-00000000-00000000-00000000-00000000-00000000-00000000-00000000-00000000"
                + "|00000001-00000000-00000000-00000000-00000000-00000000-00000000-00000000-00000000",
                "00000100-00000000-00000000-00000000-00000000-00000000-00000000-00000000-00000000"
                + "|00000011-00000000-00000000-00000000-00000000-00000000-00000000-00000000-00000000",
                "00000110-00000000-00000000-00000000-00000000-00000000-00000000-00000000-00000000"
                + "|00000111-00000000-00000000-00000000-00000000-00000000-00000000-00000000-00000000"
            );
    }

    [PropertyTest]
    // [PropertyTest(Replay = "1982901546,297288611", MaxTest = 1)]
    internal void ShouldCompressAndDecompress(PendingGameInputs gameInput)
    {
        GameInput lastAcked = new(new byte[GameInputBuffer.Capacity])
        {
            Frame = Frame.Zero,
        };

        var compressed = GetCompressedMsg(in lastAcked, gameInput.Values);

        var decompressedInputs = DecompressToList(compressed, lastAcked);

        decompressedInputs.Should().BeEquivalentTo(gameInput.Values);
    }

    [PropertyTest]
    internal bool CompressEmpty(GameInput gameInput)
    {
        GameInput lastAcked = gameInput with
        {
            Frame = Frame.Zero,
        };
        gameInput.Frame = new(1);

        var twinInput = gameInput with
        {
            Frame = new(2),
        };

        GameInput[] inputs = [gameInput, twinInput];

        var compressed = GetCompressedMsg(in lastAcked, inputs);

        ReadOnlySpan<byte> bits = compressed.Bits;
        return bits.ToArray().All(b => b is 0);
    }

    static InputMsg GetCompressedMsg(in GameInput lastAcked, params GameInput[] pendingInputs)
    {
        InputMsg inputMsg = new();

        InputEncoder encoder = new();
        var compressor = encoder.Compress(in lastAcked, ref inputMsg);

        foreach (var t in pendingInputs)
            compressor.WriteInput(in t);

        compressor.Count.Should().Be(pendingInputs.Length);
        return inputMsg;
    }

    static IReadOnlyList<GameInput> DecompressToList(InputMsg inputMsg, GameInput lastRecv)
    {
        List<GameInput> inputs = [];
        InputEncoder encoder = new();
        var decompressor = encoder.Decompress(ref inputMsg, ref lastRecv);
        while (decompressor.NextInput())
            inputs.Add(lastRecv);

        return inputs;
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

        result.Frame = new(frame);
        return result;
    }
}
