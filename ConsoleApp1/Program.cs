using System.Runtime.InteropServices;
using System.Text.Json;
using nGGPO.Serialization;
using nGGPO.Serialization.Buffer;
using nGGPO.Utils;

void Dump(in ReadOnlySpan<byte> bytes, string source = "")
{
    Console.Write("bin> ");
    foreach (var b in bytes)
    {
        Console.Write(b);
        Console.Write(' ');
    }

    Console.Write($"{{ Source: {source}; Size: {bytes.Length} }}");
    Console.WriteLine();
}

var data = new byte[] {1, 2, 3, 4, 5};
Input packet = new()
{
    S = data.Length,
    A = (byte) 'a',
    B = 2,
    Bits = data,
};

InputSerializer serializer = new();

var sizeM = Marshal.SizeOf(packet);
var size = serializer.SizeOf(packet);

Console.Clear();
Console.WriteLine($"# Size={size}, SizeM={sizeM}\n");

using var bufferMarshal = Mem.StructToBytes(packet);

serializer.Network = false;
using var buffer = serializer.Serialize(packet);

serializer.Network = true;
using var bufferNetWork = serializer.Serialize(packet);

Dump(bufferMarshal, "Marshall");
Dump(buffer, "Serial");
Dump(bufferNetWork, "Network");

var valueMarshall = Mem.BytesToStruct<Input>(bufferMarshal);
serializer.Network = false;
var value = serializer.Deserialize(buffer);
serializer.Network = true;
var valueNetwork = serializer.Deserialize(bufferNetWork);

Console.WriteLine();
Console.WriteLine(valueMarshall);
Console.WriteLine(value);
Console.WriteLine(valueNetwork);

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Input
{
    public int S;
    public byte A;
    public uint B;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
    public byte[] Bits; /* must be last */

    public override string ToString() =>
        JsonSerializer.Serialize(this, new JsonSerializerOptions {IncludeFields = true});
}

class InputSerializer : BinarySerializer<Input>
{
    public override int SizeOf(in Input data) =>
        sizeof(int) + sizeof(byte) + sizeof(uint) +
        data.Bits.Length * sizeof(byte);

    protected override void Serialize(ref NetworkBufferWriter writer, in Input data)
    {
        writer.Write(data.S);
        writer.Write(data.A);
        writer.Write(data.B);
        writer.Write(data.Bits);
    }

    protected override Input Deserialize(ref NetworkBufferReader reader)
    {
        var size = reader.ReadInt();
        var input = new Input
        {
            S = size,
            A = reader.ReadByte(),
            B = reader.ReadUInt(),
            Bits = new byte[size],
        };
        reader.ReadByte(input.Bits);
        return input;
    }
}

record Foo(string A);