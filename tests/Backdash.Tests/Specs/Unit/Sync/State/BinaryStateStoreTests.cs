using System.Numerics;
using Backdash.Data;
using Backdash.Serialization;
using Backdash.Sync.State.Stores;
using Backdash.Tests.TestUtils;

namespace Backdash.Tests.Specs.Unit.Sync.State;

public class BinaryStateStoreTests
{
    [Fact]
    public void ShouldInitializeCorrectly()
    {
        BinaryStateStore<GameState> store = new(GameStateSerializer.Shared, 40);
        store.Initialize(1);

        var newState = GameState.CreateRandom();
        ref var currentState = ref store.GetCurrent();
        currentState = newState;

        store.SaveCurrent(Frame.One, 0);

        ref readonly var loaded = ref store.Load(Frame.One);
        loaded.GameState.Should().BeEquivalentTo(newState);
    }

    [Fact]
    public void ShouldInitializeResizingHintSizeBufferCorrectly()
    {
        BinaryStateStore<GameState> store = new(GameStateSerializer.Shared, 1);
        store.Initialize(1);

        var newState = GameState.CreateRandom();
        ref var currentState = ref store.GetCurrent();
        currentState = newState;

        store.SaveCurrent(Frame.One, 0);

        ref readonly var loaded = ref store.Load(Frame.One);
        loaded.GameState.Should().BeEquivalentTo(newState);
    }
}

public record GameState
{
    public int Value1;
    public long Value2;
    public bool Value3;
    public Vector2 Value4;
    public Vector3 Value5;
    public byte[] MoreValues = new byte[3];

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

[StateSerializer<GameState>]
public partial class GameStateSerializer;
