using Backdash.Core;
using Backdash.Data;
using Backdash.Network.Messages;
using Backdash.Network.Protocol.Messaging;
using Backdash.Sync;

namespace Backdash.Tests.Specs.Unit.Input;

public class InputEncoderTests
{
    [Fact]
    public void ShouldCompressAndDecompressSample()
    {
        var lastAcked = Generate.GameInput(0, [1, 0]);

        GameInput[] nextInputs =
        [
            Generate.GameInput(1, [0, 2]),
            Generate.GameInput(2, [2, 4]),
            Generate.GameInput(3, [4, 8]),
        ];

        var compressed = GetCompressedInput(in lastAcked, nextInputs);
        var decompressedInputs = DecompressToList(compressed, lastAcked);

        decompressedInputs.Should().BeEquivalentTo(nextInputs);

        decompressedInputs
            .Select(x => x.Buffer.ToString())
            .Should().BeEquivalentTo(
                "00000000-00000010",
                "00000010-00000100",
                "00000100-00001000"
            );
    }

    [Fact]
    public void ShouldCompressAndDecompressSampleSkippingFrames()
    {
        var lastAcked = Generate.GameInput(0, [1, 0]);

        GameInput[] sendInputs =
        [
            Generate.GameInput(1, [0, 2]),
            Generate.GameInput(2, [2, 4]),
            Generate.GameInput(3, [4, 8]),
            Generate.GameInput(4, [8, 16]),
        ];

        var compressed = GetCompressedInput(in lastAcked, sendInputs);

        var lastReceived = Generate.GameInput(2, [2, 4]);
        var decompressedInputs = DecompressToList(compressed, lastReceived);

        decompressedInputs.Should().BeEquivalentTo([
            Generate.GameInput(3, [4, 8]),
            Generate.GameInput(4, [8, 16]),
        ]);

        decompressedInputs
            .Select(x => x.Buffer.ToString())
            .Should().BeEquivalentTo(
                "00000100-00001000",
                "00001000-00010000"
            );
    }

    [Fact]
    public void ShouldCompressAndDecompressSample2()
    {
        var lastAcked = Generate.GameInput(0, [0]);

        GameInput[] inputList =
        [
            Generate.GameInput(1, [4]),
            Generate.GameInput(2, [16]),
        ];

        var compressed = GetCompressedInput(in lastAcked, inputList);

        var decompressedInputs = DecompressToList(compressed, lastAcked);

        decompressedInputs.Should().BeEquivalentTo(inputList);
    }

    [PropertyTest]
    // [PropertyTest(Replay = "1982901546,297288611", MaxTest = 1)]
    internal void ShouldCompressAndDecompress(PendingGameInputs gameInput)
    {
        GameInput lastAcked = new(new byte[Max.TotalInputSizeInBytes])
        {
            Frame = Frame.Zero,
        };

        var compressed = GetCompressedInput(in lastAcked, gameInput.Values);

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

        var compressed = GetCompressedInput(in lastAcked, inputs);

        ReadOnlySpan<byte> bits = compressed.Bits;
        return bits.ToArray().All(b => b is 0);
    }

    static InputMessage GetCompressedInput(in GameInput lastAcked, params GameInput[] pendingInputs)
    {
        InputMessage inputMsg = new();

        if (pendingInputs is [var first, ..])
        {
            inputMsg.InputSize = (byte)first.Size;
            inputMsg.StartFrame = first.Frame;
        }

        Span<byte> lastBytes = stackalloc byte[lastAcked.Size];
        lastAcked.CopyTo(lastBytes);
        var compressor = InputEncoder.GetCompressor(ref inputMsg, lastBytes);

        for (var i = 0; i < pendingInputs.Length; i++)
        {
            ref var t = ref pendingInputs[i];
            if (!compressor.Write(t.Buffer[..t.Size]))
                throw new InvalidOperationException();
        }

        compressor.Count.Should().Be(pendingInputs.Length);
        inputMsg.NumBits = compressor.BitOffset;
        return inputMsg;
    }

    internal static IReadOnlyList<GameInput> DecompressToList(InputMessage inputMsg) =>
        DecompressToList(inputMsg, GameInput.CreateEmpty());

    // LATER: after encoding refactoring this ends having too many logic, must be improved
    internal static IReadOnlyList<GameInput> DecompressToList(InputMessage inputMsg, GameInput lastRecv)
    {
        List<GameInput> inputs = [];

        lastRecv.Size = inputMsg.InputSize;
        if (lastRecv.Frame.IsNull)
            lastRecv.Frame = inputMsg.StartFrame.Previous();

        var currentFrame = inputMsg.StartFrame;
        var nextFrame = lastRecv.Frame.Next();
        currentFrame.Number.Should().BeLessOrEqualTo(nextFrame.Number);
        var decompressor = InputEncoder.GetDecompressor(ref inputMsg);

        var framesAhead = nextFrame.Number - currentFrame.Number;
        if (decompressor.Skip(framesAhead))
            currentFrame += framesAhead;

        currentFrame.Should().Be(nextFrame);
        while (decompressor.Read(lastRecv.Buffer))
        {
            currentFrame.Number.Should().Be(lastRecv.Frame.Next().Number);
            lastRecv.Frame = currentFrame;
            currentFrame++;
            inputs.Add(lastRecv);
        }

        return inputs;
    }
}
