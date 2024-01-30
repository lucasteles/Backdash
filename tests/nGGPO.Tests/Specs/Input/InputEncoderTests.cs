using System.Threading.Channels;
using nGGPO.Input;
using nGGPO.Network.Messages;
using nGGPO.Network.Protocol.Internal;

namespace nGGPO.Tests.Specs.Input;

public class InputEncoderTests
{
    [Fact]
    public async Task Test1()
    {
        var lastAcked = CreateInput(0, [1]);
        var lastSent = GameInput.Empty;

        GameInput[] inputList =
        [
            CreateInput(1, [2], [1]),
            CreateInput(2, [4], [3]),
            CreateInput(3, [6], [7])
        ];

        var pendingInputs = await CreateBuffer(inputList);

        var compressed = InputEncoder.Compress(
            pendingInputs,
            in lastAcked,
            ref lastSent,
            out var count
        );

        var decompressedInputs = DecompressToList(compressed);

        decompressedInputs.Should().BeEquivalentTo(inputList);
        count.Should().Be(inputList.Length);
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

    static async Task<ChannelReader<T>> CreateBuffer<T>(params T[] values) where T : notnull
    {
        var res = Channel.CreateUnbounded<T>();
        foreach (var v in values) await res.Writer.WriteAsync(v);
        await res.Reader.WaitToReadAsync();
        res.Writer.Complete();
        return res;
    }

    static IReadOnlyList<GameInput> DecompressToList(InputMsg inputMsg)
    {
        List<GameInput> inputs = [];
        GameInput lastRecv = GameInput.Empty;
        var decompressor = InputEncoder.Decompress(ref inputMsg, ref lastRecv);
        while (decompressor.NextInput())
            inputs.Add(lastRecv);

        return inputs;
    }
}
