using Backdash.Serialization;
using Backdash.Serialization.Buffer;
namespace Backdash.Sync.Input.Spectator;
sealed class CombinedInputsSerializer<T>(IBinarySerializer<T> inputSerializer)
    : BinarySerializer<CombinedInputs<T>> where T : struct
{
    protected override void Serialize(in BinarySpanWriter binaryWriter, in CombinedInputs<T> data)
    {
        binaryWriter.Write(data.Count);
        for (var i = 0; i < data.Count; i++)
        {
            var size = inputSerializer.Serialize(data.Inputs[i], binaryWriter.CurrentBuffer);
            binaryWriter.Advance(size);
        }
    }
    protected override void Deserialize(in BinarySpanReader binaryReader, ref CombinedInputs<T> result)
    {
        result.Count = binaryReader.ReadByte();
        for (var i = 0; i < result.Count; i++)
        {
            var size = inputSerializer.Deserialize(binaryReader.CurrentBuffer, ref result.Inputs[i]);
            binaryReader.Advance(size);
        }
    }
}
