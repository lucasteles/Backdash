using Backdash.Serialization;
using Backdash.Serialization.Buffer;

namespace Backdash.Sync.Input.Spectator;

sealed class InputGroupSerializer<T>(IBinarySerializer<T> inputSerializer)
    : BinarySerializer<InputGroup<T>> where T : struct
{
    protected override void Serialize(in BinarySpanWriter writer, in InputGroup<T> data)
    {
        writer.Write(data.Count);
        for (var i = 0; i < data.Count; i++)
        {
            var size = inputSerializer.Serialize(data.Inputs[i], writer.CurrentBuffer);
            writer.Advance(size);
        }
    }

    protected override void Deserialize(in BinarySpanReader reader, ref InputGroup<T> result)
    {
        result.Count = reader.ReadByte();

        for (var i = 0; i < result.Count; i++)
        {
            var size = inputSerializer.Deserialize(reader.CurrentBuffer, ref result.Inputs[i]);
            reader.Advance(size);
        }
    }
}
