using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using nGGPO.DataStructure;
using nGGPO.Inputs;
using nGGPO.Serialization;
using nGGPO.Serialization.Buffer;
using nGGPO.Utils;

void Div() => Console.WriteLine(new string('-', 10));

var channel = CircularBuffer.CreateChannel<int>(3);
var w = channel.Writer;
await w.WriteAsync(10);
await w.WriteAsync(20);
await w.WriteAsync(30);
await w.WriteAsync(40);
await w.WriteAsync(50);
w.Complete();

Console.WriteLine();
await foreach (var v in channel.Reader.ReadAllAsync())
    Console.Write($"{v},");
Console.WriteLine();
// byte[] data = {1, 2, 3, 4, 5};
//
// {
//     var packet = long.MaxValue;
//     var serializer = BinarySerializers.Get<long>()!;
//     using var buffer = MemoryBuffer.Rent(10, true);
//     var size = serializer.Serialize(ref packet, buffer);
//     var bytes = buffer[..size];
//
//     Console.WriteLine($"# Size={size}\n");
//     var backPacket = serializer.Deserialize(bytes);
//     Console.WriteLine($"# Pkg={backPacket}\n");
// }
// Div();
// {
//     var packet = ButtonsInput.UpLeft | ButtonsInput.X;
//     var serializer = BinarySerializers.Get<ButtonsInput>()!;
//     using var buffer = MemoryBuffer.Rent(10, true);
//     var size = serializer.Serialize(ref packet, buffer);
//     var bytes = buffer[..size];
//     Console.WriteLine($"# Size={size}\n");
//     var backPacket = serializer.Deserialize(bytes);
//     var buttons = new ButtonsInputEditor(backPacket);
//     Console.WriteLine($"# Pkg= {buttons}\n");
// }
// Div();
// {
//     Input packet = new()
//     {
//         S = data.Length,
//         A = (byte) 'a',
//         B = 2,
//         Bits = new(),
//     };
//     data.CopyTo(packet.Bits);
//     Console.WriteLine($"# Ipt: {packet}\n");
//
//
//     var serializer = BinarySerializers.Get<Input>()!;
//     using var buffer = MemoryBuffer.Rent(20, true);
//     var size = serializer.Serialize(ref packet, buffer);
//     var bytes = buffer[..size];
//
//     Console.WriteLine($"# Size={size}\n");
//     var backPacket = serializer.Deserialize(bytes);
//     Console.WriteLine($"# Pkg: {backPacket}\n");
// }
// Div();
// {
//     Input packet = new()
//     {
//         S = data.Length,
//         A = (byte) 'a',
//         B = 2,
//         Bits = new(),
//     };
//     data.CopyTo(packet.Bits);
//
//     var serializer = new CustomInputSerializer {Network = false};
//
//     using var buffer = MemoryBuffer.Rent(20, true);
//     var size = serializer.Serialize(ref packet, buffer);
//     var bytes = buffer[..size];
//
//     Console.WriteLine($"# Size={size}\n");
//     var backPacket = serializer.Deserialize(bytes);
//     Console.WriteLine($"# Pkg={backPacket}\n");
// }
//
// [InlineArray(10)]
// [DebuggerDisplay("Buffer {ToString()}")]
// public struct ValueBuffer
// {
// #pragma warning disable CS0169 // Field is never used
//     byte element0;
// #pragma warning restore CS0169 // Field is never used
//
//     public override string ToString()
//     {
//         ReadOnlySpan<byte> bytes = this;
//         return $"[{string.Join(", ", bytes.ToArray().Select(x => (int) x))}]";
//     }
// }
//
// [StructLayout(LayoutKind.Sequential, Pack = 1)]
// public struct Input
// {
//     public int S;
//     public byte A;
//     public uint B;
//
//     [JsonIgnore]
//     public ValueBuffer Bits;
//
//     // [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
//     // public byte[] Bits; /* must be last */
//
//     public override string ToString() =>
//         $"{JsonSerializer.Serialize(this, new JsonSerializerOptions {IncludeFields = true})}; Buffer: {Bits}";
// }
//
// class CustomInputSerializer : BinarySerializer<Input>
// {
//     protected override void Serialize(scoped NetworkBufferWriter writer, in Input data)
//     {
//         writer.Write(data.S);
//         writer.Write(data.Bits, data.S);
//         writer.Write(data.A);
//         writer.Write(data.B);
//     }
//
//     protected override Input Deserialize(scoped NetworkBufferReader reader)
//     {
//         var size = reader.ReadInt();
//
//         var bits = new ValueBuffer();
//         reader.ReadByte(bits, size);
//
//         return new()
//         {
//             S = size,
//             Bits = bits,
//             A = reader.ReadByte(),
//             B = reader.ReadUInt(),
//         };
//     }
// }