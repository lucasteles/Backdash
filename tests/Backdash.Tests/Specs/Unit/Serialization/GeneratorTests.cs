using System.Numerics;
using Backdash.Data;
using Backdash.Serialization;

namespace Backdash.Tests.Specs.Unit.Serialization;

public class SubState
{
    public short Sub1;
    public long Sub2;
}

public record struct MyVector2(float X, float Y);

public class GameState
{
    public int Value1;
    public long Value2;
    public bool Value3;

    public Vector2 Value4;
    public SubState Value5 = new();

    public int[] Value6 = new int[5];

    public MyVector2[] Value7 = new MyVector2[3];
    public Array<MyVector2> Value8;

    public GameState()
    {
        Value8 = new(Value7.Length);
        for (int i = 0; i < Value7.Length; i++)
        {
            Value7[i] = new();
            Value8[i] = new();
        }
    }
}

[StateSerializer<MyVector2>]
public partial class MyVector2Serializer;

[StateSerializer<SubState>]
public partial class SubStateSerializer;

[StateSerializer<GameState>]
public partial class GameStateSerializer;

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
            Value5 = new()
            {
                Sub1 = -1,
                Sub2 = 99,
            },
            Value6 = [89, 78, 11, 65, 789],
            Value7 =
            [
                new(-2, 98),
                new(-3, 97),
                new(-4, 96),
            ],
            Value8 =
            [
                new(-12, 198),
                new(-13, 197),
                new(-14, 196),
            ]
        };

        var size = serializer.Serialize(in data, buffer);

        GameState result = new();
        serializer.Deserialize(buffer[..size], ref result);

        result.Should().BeEquivalentTo(data);
    }
}
