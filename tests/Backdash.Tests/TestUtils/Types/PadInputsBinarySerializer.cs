﻿using Backdash.GamePad;
using Backdash.Serialization;
using Backdash.Serialization.Buffer;

namespace Backdash.Tests.TestUtils.Types;

public class PadInputsBinarySerializer : BinarySerializer<PadInputs>
{
    protected override void Serialize(in BinarySpanWriter binaryWriter, in PadInputs data)
    {
        binaryWriter.Write((short)data.Buttons);
        binaryWriter.Write(data.LeftTrigger);
        binaryWriter.Write(data.LeftTrigger);

        binaryWriter.Write(data.LeftAxis.X);
        binaryWriter.Write(data.LeftAxis.Y);

        binaryWriter.Write(data.RightAxis.X);
        binaryWriter.Write(data.RightAxis.Y);
    }

    protected override void Deserialize(in BinarySpanReader binaryReader, ref PadInputs result)
    {
        result.Buttons = (PadInputs.PadButtons)binaryReader.ReadShort();
        result.LeftTrigger = binaryReader.ReadByte();
        result.RightTrigger = binaryReader.ReadByte();

        result.LeftAxis.X = binaryReader.ReadSByte();
        result.LeftAxis.Y = binaryReader.ReadSByte();

        result.RightAxis.X = binaryReader.ReadSByte();
        result.RightAxis.Y = binaryReader.ReadSByte();
    }
}
