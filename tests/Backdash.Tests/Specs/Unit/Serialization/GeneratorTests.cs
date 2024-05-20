using Backdash.Serialization;

namespace Backdash.Tests.Specs.Unit.Serialization;

[StateSerializer<GameState>]
public partial class GameStateSerializer;

public class GameState
{
    public int Value1;
    public long Value2;
    public bool Value3;
}

public class GeneratorTests { }
