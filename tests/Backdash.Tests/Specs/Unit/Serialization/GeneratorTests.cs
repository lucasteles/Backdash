using System.Buffers;
using System.Numerics;
using Backdash.Serialization;
using Backdash.Serialization.Buffer;

namespace Backdash.Tests.Specs.Unit.Serialization;

public class SubState
{
    public short Sub1;
    public long Sub2;
}

public record struct MyVector2(float X, float Y);

public enum EnumState : uint { None, Foo, Bar }

public class GameState
{
    public int Value1;
    public long Value2;
    public bool Value3;

    public Vector2 Value4;
    public SubState Value5 = new();

    public int[] Value6 = new int[5];

    public MyVector2[] Value7 = new MyVector2[3];
    public EnumState Value9;
    public EnumState[] Value10 = new EnumState[2];

    public GameState()
    {
        for (int i = 0; i < Value7.Length; i++)
        {
            Value7[i] = new();
        }
    }
}

[BinarySerializer<MyVector2>]
public partial class MyVector2Serializer;

[BinarySerializer<SubState>]
public partial class SubStateSerializer;

[BinarySerializer<GameState>]
public partial class GameStateSerializer;

public class GeneratorTests
{
    [Fact]
    public void ShouldSerializeDeserialize()
    {
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
            Value9 = EnumState.Foo,
            Value10 = [EnumState.Foo, EnumState.Bar],
        };

        ArrayBufferWriter<byte> buffer = new();
        serializer.Serialize(new(buffer), in data);

        GameState result = new();
        var offset = 0;
        BinaryBufferReader reader = new(buffer.WrittenSpan, ref offset);
        serializer.Deserialize(reader, ref result);

        result.Should().BeEquivalentTo(data);
    }
}
