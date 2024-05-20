using System.Numerics;
using Backdash.Serialization;

namespace Backdash.Tests.Specs.Unit.Serialization;

[StateSerializer<GameState>]
public partial class GameStateSerializer;

public class SubState
{
    public short Sub1;
    public long Sub2;
}

public class GameState
{
    public int Value1;
    public long Value2;
    public bool Value3;

    public Vector2 Value4;
    // public SubState? Value5;
}

public class GeneratorTests
{
    [Fact]
    public void ShouldSerializeDeserialize()
    {
        Span<byte> buffer = new byte[1024];

        var serializer = GameStateSerializer.Shared;

        GameState data = new()
        {
            Value1 = 42,
            Value2 = 1,
            Value3 = true,
            Value4 = new(20, 30),
        };

        var size = serializer.Serialize(in data, buffer);

        GameState result = new();
        serializer.Deserialize(buffer[..size], ref result);

        result.Should().BeEquivalentTo(data);
    }
}
