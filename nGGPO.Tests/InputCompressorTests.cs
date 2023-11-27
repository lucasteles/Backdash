using nGGPO.DataStructure;
using nGGPO.Input;
using nGGPO.Network;

namespace nGGPO.Tests;

public class InputCompressorTests
{
    [Fact]
    public void Test1()
    {
        var lastAcked = CreateInput(0, 1);
        var lastSent = GameInput.Empty;

        var pendingInputs = CreateBuffer(
            CreateInput(1, 2),
            CreateInput(2, 4)
        );

        var compressed = InputCompressor.WriteCompressed(
            ref lastAcked,
            in pendingInputs,
            ref lastSent
        );
    }

    static GameInput CreateInput(int frame, params byte[] value)
    {
        var result = new GameInput(value);
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
