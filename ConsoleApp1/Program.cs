using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using nGGPO.Serialization;
using nGGPO.Serialization.Buffer;
using static ConsoleApp1.Helpers;
using nGGPO.Utils;


var data = new byte[] {1, 2, 3, 4, 5};

Input packet = new()
{
    S = data.Length,
    A = (byte) 'a',
    B = 2,
    Values = new(),
    // Bits = data,
};

Span<byte> values = packet.Values;
data.CopyTo(values);

var serializers = BinarySerializers.Get<Input>();

using var buffer = Mem.Rent(20);
var size = serializers.Serialize(in packet, buffer);
var bytes = buffer.Span[..size];

Console.WriteLine($"# Size={size}\n");
var backPacket = serializers.Deserialize(bytes);

Console.WriteLine($"# Pkg={backPacket}\n");
Console.Clear();

// Console.WriteLine($"# Size={size}, SizeM={sizeM}\n");

// using var bufferMarshal = Mem.StructToBytes(packet);

// serializer.Network = false;
// using var buffer = serializer.Serialize(packet);
//
// serializer.Network = true;
// using var bufferNetWork = serializer.Serialize(packet);
//
// Dump(bufferMarshal, "Marshall");
// Dump(buffer, "Serial");
// Dump(bufferNetWork, "Network");
//
// var valueMarshall = Mem.BytesToStruct<Input>(bufferMarshal);
// serializer.Network = false;
// var value = serializer.Deserialize(buffer);
// serializer.Network = true;
// var valueNetwork = serializer.Deserialize(bufferNetWork);
//
// Console.WriteLine();
// Console.WriteLine(valueMarshall);
// Console.WriteLine(value);
// Console.WriteLine(valueNetwork);

[InlineArray(5)]
[DebuggerDisplay("Buffer {ToString()}")]
public struct ValueBuffer
{
#pragma warning disable CS0169 // Field is never used
    byte element0;
#pragma warning restore CS0169 // Field is never used

    public override string ToString()
    {
        ReadOnlySpan<byte> bytes = this;
        return $"[{string.Join(", ", bytes.ToArray())}]";
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Input
{
    public int S;
    public byte A;
    public uint B;

    public ValueBuffer Values;
    // [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
    // public byte[] Bits; /* must be last */

    public override string ToString() =>
        JsonSerializer.Serialize(this, new JsonSerializerOptions {IncludeFields = true});
}

// class InputSerializer : BinarySerializer<Input>
// {
//     public override int SizeOf(in Input data) =>
//         sizeof(int) + sizeof(byte) + sizeof(uint)
//         + data.Bits.Length * sizeof(byte);
//
//     protected override void Serialize(ref NetworkBufferWriter writer, in Input data)
//     {
//         writer.Write(data.S);
//         writer.Write(data.A);
//         writer.Write(data.B);
//         writer.Write(data.Bits);
//     }
//
//     protected override Input Deserialize(ref NetworkBufferReader reader)
//     {
//         var size = reader.ReadInt();
//         var input = new Input
//         {
//             S = size,
//             A = reader.ReadByte(),
//             B = reader.ReadUInt(),
//             Bits = new byte[size],
//         };
//         reader.ReadByte(input.Bits);
//         return input;
//     }
// }