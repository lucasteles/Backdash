using nGGPO.Serialization;
using nGGPO.Serialization.Buffer;

class CustomInputSerializer : BinarySerializer<Input>
{
    protected override void Serialize(scoped NetworkBufferWriter writer, in Input data)
    {
        writer.Write(data.S);
        writer.Write(data.Bits, data.S);
        writer.Write(data.A);
        writer.Write(data.B);
    }

    protected override Input Deserialize(scoped NetworkBufferReader reader)
    {
        var size = reader.ReadInt();

        var bits = new ValueBuffer();
        reader.ReadByte(bits, size);

        return new()
        {
            S = size,
            Bits = bits,
            A = reader.ReadByte(),
            B = reader.ReadUInt(),
        };
    }
}