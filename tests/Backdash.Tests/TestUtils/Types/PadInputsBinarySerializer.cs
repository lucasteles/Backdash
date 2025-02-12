using Backdash.Serialization;
using Backdash.Serialization.Buffer;

namespace Backdash.Tests.TestUtils.Types;

public class PadInputsBinarySerializer : BinarySerializer<PadInputs>
{
    protected override void Serialize(in BinarySpanWriter binaryWriter, in PadInputs data)
    {
        binaryWriter.Write((short)data.Buttons);
        binaryWriter.Write(data.LeftTrigger);
        binaryWriter.Write(data.RightTrigger);

        binaryWriter.Write(data.LeftAxis.X);
        binaryWriter.Write(data.LeftAxis.Y);

        binaryWriter.Write(data.RightAxis.X);
        binaryWriter.Write(data.RightAxis.Y);
    }

    protected override void Deserialize(in BinaryBufferReader binaryReader, ref PadInputs result)
    {
        result.Buttons = (PadInputs.PadButtons)binaryReader.ReadInt16();
        result.LeftTrigger = binaryReader.ReadByte();
        result.RightTrigger = binaryReader.ReadByte();

        result.LeftAxis.X = binaryReader.ReadSByte();
        result.LeftAxis.Y = binaryReader.ReadSByte();

        result.RightAxis.X = binaryReader.ReadSByte();
        result.RightAxis.Y = binaryReader.ReadSByte();
    }
}
