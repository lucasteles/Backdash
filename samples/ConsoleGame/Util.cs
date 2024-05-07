using Backdash.Serialization;
using Backdash.Serialization.Buffer;
namespace ConsoleGame;

public sealed class MyStateSerializer : BinarySerializer<GameState>
{
    protected override void Serialize(in BinarySpanWriter binaryWriter, in GameState data)
    {
        binaryWriter.Write(data.Position1);
        binaryWriter.Write(data.Position2);
    }
    protected override void Deserialize(in BinarySpanReader binaryReader, ref GameState result)
    {
        result.Position1 = binaryReader.ReadVector2();
        result.Position2 = binaryReader.ReadVector2();
    }
}
