using System.Runtime.InteropServices;
using System.Text.Json;
using nGGPO;

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

var sizeM = Marshal.SizeOf(packet);
var size = packet.Size();

Console.Clear();
Console.WriteLine($"# Size={size}, SizeM={sizeM}\n");

var bufferMarshal = Mem.SerializeMarshal(packet);
var bufferRaw = packet.SerializeManual();
var buffer = packet.Serialize();
var bufferNetWork = packet.Serialize(true);

Dump(bufferMarshal, "Marshall");
Dump(bufferRaw, "Raw");
Dump(buffer, "Serial");
Dump(bufferNetWork, "Network");

var valueMarshall = Mem.DeserializeMarshal<Input>(bufferMarshal);
var valueRaw = Mem.DeserializeMarshal<Input>(bufferRaw);
var value = Input.Deserialize(buffer);
var valueNetwork = Input.Deserialize(bufferNetWork, true);

Console.WriteLine();
Console.WriteLine(valueMarshall);
Console.WriteLine(valueRaw);
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

    public int Size() => sizeof(int) + sizeof(byte) + sizeof(uint) + Bits.Length * sizeof(byte);

    public byte[] SerializeManual()
    {
        var buffer = new byte[Size()];
        var offset = 0;

        var s = BitConverter.GetBytes(S);
        foreach (var t in s)
            buffer[offset++] = t;

        buffer[offset++] = A;
        var b = BitConverter.GetBytes(B);
        foreach (var t in b)
            buffer[offset++] = t;

        foreach (var t in Bits)
            buffer[offset++] = t;

        return buffer;
    }

    public byte[] Serialize(bool network = false)
    {
        var buffer = new byte[Size()];
        NetworkBufferWriter writer = new(buffer, network);
        writer.Write(S);
        writer.Write(A);
        writer.Write(B);
        writer.Write(Bits);
        return buffer;
    }

    public static Input Deserialize(byte[] bytes, bool network = false)
    {
        NetworkBufferReader reader = new(bytes, network);
        var size = reader.ReadInt();
        return new()
        {
            S = size,
            A = reader.Read(),
            B = reader.ReadUInt(),
            Bits = reader.Read(size),
        };
    }


    public override string ToString() =>
        JsonSerializer.Serialize(this, new JsonSerializerOptions {IncludeFields = true});
}