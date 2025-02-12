using Backdash.Serialization;
using Backdash.Serialization.Buffer;

namespace Backdash.Synchronizing.Input.Confirmed;

sealed class ConfirmedInputsSerializer<T>(IBinarySerializer<T> inputSerializer)
    : BinarySerializer<ConfirmedInputs<T>> where T : unmanaged
{
    protected override void Serialize(in BinarySpanWriter binaryWriter, in ConfirmedInputs<T> data)
    {
        binaryWriter.Write(data.Count);
        for (var i = 0; i < data.Count; i++)
        {
            var size = inputSerializer.Serialize(data.Inputs[i], binaryWriter.CurrentBuffer);
            binaryWriter.Advance(size);
        }
    }

    protected override void Deserialize(in BinaryBufferReader binaryReader, ref ConfirmedInputs<T> result)
    {
        result.Count = binaryReader.ReadByte();
        for (var i = 0; i < result.Count; i++)
        {
            var size = inputSerializer.Deserialize(binaryReader.CurrentBuffer, ref result.Inputs[i]);
            binaryReader.Advance(size);
        }
    }
}
