#pragma warning disable S1854
// ReSharper disable UnassignedField.Global
using System.Runtime.InteropServices;
using Backdash.Network;
using Backdash.Serialization.Buffer;

namespace Backdash.Benchmarks.Cases;

[RPlotExporter]
[InProcess, MemoryDiagnoser]
public class ReadWriteIntegerBenchmark
{
    [Params(Endianness.LittleEndian, Endianness.BigEndian)]
    public Endianness Mode;

    [Params(1, int.MaxValue / 2, int.MaxValue)]
    public int Number;

    const int Count = 100_000;

    [Benchmark]
    public void WriteInt32Bytes()
    {
        Span<byte> span = stackalloc byte[sizeof(int)];
        var offset = 0;
        BinaryRawBufferWriter writer = new(span, ref offset)
        {
            Endianness = Mode,
        };
        for (var i = 0; i < Count; i++)
        {
            writer.Write(Number);
            offset = 0;
        }
    }

    [Benchmark]
    public void ReadInt32Bytes()
    {
        Span<byte> span = stackalloc byte[sizeof(int)];
        var offset = 0;
        BinaryBufferReader reader = new(span, ref offset)
        {
            Endianness = Mode,
        };
        for (var i = 0; i < Count; i++)
        {
            offset = 0;
            MemoryMarshal.Write(span, Number);
            reader.ReadInt32();
        }
    }
}
