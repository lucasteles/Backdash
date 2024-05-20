﻿using System.Numerics;
using Backdash.Serialization;

namespace Backdash.Tests.Specs.Unit.Serialization;

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
    public SubState Value5 = new();

    public int[] Value6 = new int[5];

    public SubState[] Value7 = new SubState[3];

    public GameState()
    {
        for (int i = 0; i < Value7.Length; i++)
            Value7[i] = new();
    }
}

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
                new()
                {
                    Sub1 = -2,
                    Sub2 = 98,
                },
                new()
                {
                    Sub1 = -3,
                    Sub2 = 97,
                },
                new()
                {
                    Sub1 = -4,
                    Sub2 = 96,
                },
            ]
        };

        var size = serializer.Serialize(in data, buffer);

        GameState result = new();
        serializer.Deserialize(buffer[..size], ref result);

        result.Should().BeEquivalentTo(data);
    }
}
