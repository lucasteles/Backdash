using System.Numerics;
using Backdash.Serialization;
using Backdash.Synchronizing.State;
using Backdash.Tests.TestUtils;

namespace Backdash.Tests.Specs.Unit.Sync.State;

public class DefaultStateStoreTests
{
    [Fact]
    public void ShouldInitializeCorrectly()
    {
        DefaultStateStore store = new(40);
        store.Initialize(1);

        ref var currentState = ref store.Next();
        currentState.Frame = Frame.One;
        currentState.Checksum = 0;

        var gameState = GameState.CreateRandom();
        GameStateSerializer.Shared.Serialize(new(currentState.GameState), in gameState);

        if (!store.TryLoad(Frame.One, out var loaded))
            Assert.Fail("Failed to load state.");

        GameState newGameState = new();

        var offset = 0;
        BinaryBufferReader reader = new(loaded.GameState.WrittenSpan, ref offset);
        GameStateSerializer.Shared.Deserialize(reader, ref newGameState);

        newGameState.Should().BeEquivalentTo(gameState);
    }
}

public record GameState
{
    public int Value1;
    public long Value2;
    public bool Value3;
    public Vector2 Value4;
    public Vector3 Value5;
    public readonly byte[] MoreValues = new byte[3];

    public static GameState CreateRandom()
    {
        GameState result = new()
        {
            Value1 = Generate.Random.Int(),
            Value2 = Generate.Random.Long(),
            Value3 = Generate.Random.Bool(),
            Value4 = Generate.Vector2(),
            Value5 = Generate.Vector3(),
        };

        for (int i = 0; i < result.MoreValues.Length; i++)
            result.MoreValues[i] = Generate.Random.Byte();

        return result;
    }
}

[BinarySerializer<GameState>]
public partial class GameStateSerializer;
