using Backdash.Serialization;
using Backdash.Serialization.Buffer;
namespace ConsoleGame;
public sealed class MyStateSerializer : BinarySerializer<GameState>
{
    protected override void Serialize(in BinarySpanWriter writer, in GameState data)
    {
        writer.Write(data.Position1);
        writer.Write(data.Position2);
    }
    protected override void Deserialize(in BinarySpanReader reader, ref GameState result)
    {
        result.Position1 = reader.ReadVector2();
        result.Position2 = reader.ReadVector2();
    }
}