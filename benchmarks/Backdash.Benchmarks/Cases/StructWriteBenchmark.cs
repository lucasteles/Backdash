#pragma warning disable S1854
// ReSharper disable UnassignedField.Global
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Backdash.Core;

namespace Backdash.Benchmarks.Cases;

[RPlotExporter, RankColumn]
[InProcess, MemoryDiagnoser]
public class StructWriteSingleBenchmark
{
    static readonly int tSize = Unsafe.SizeOf<StructData>();
    readonly byte[] arrayBuffer = new byte[tSize];

    StructData data;

    [GlobalSetup]
    public void Setup()
    {
        Random random = new(42);
        data = StructData.Generate(random);
    }

    [IterationSetup]
    public void BeforeEach() => Array.Clear(arrayBuffer);

    Span<byte> GetBuffer() => arrayBuffer;

    [Benchmark]
    public void ReadWrite()
    {
        var buffer = GetBuffer();
        MemoryMarshal.Write(buffer, in data);

        var copy = MemoryMarshal.Read<StructData>(buffer);

        Debug.Assert(data == copy);
    }

    [Benchmark]
    public void AsBytes()
    {
        var buffer = GetBuffer();
        Mem.AsBytes(in data).CopyTo(buffer);

        StructData copy = default;
        buffer.CopyTo(Mem.AsBytes(in copy));

        Debug.Assert(data == copy);
    }
}

[RPlotExporter, RankColumn]
[InProcess, MemoryDiagnoser]
public class StructWriteSpanBenchmark
{
    const int N = 1_000_000;
    static readonly int tSize = Unsafe.SizeOf<StructData>();
    readonly byte[] arrayBuffer = new byte[tSize * N];

    StructData[] dataArray = [];
    StructData[] resultArray = [];

    [GlobalSetup]
    public void Setup()
    {
        Random random = new(42);
        dataArray = StructData.Generate(random, N);
        resultArray = new StructData[N];
    }

    [IterationSetup]
    public void BeforeEach()
    {
        Array.Clear(arrayBuffer);
        Array.Clear(resultArray);
    }

    Span<byte> GetBuffer() => arrayBuffer;

    [Benchmark]
    public void ReadWrite()
    {
        var buffer = GetBuffer();
        int index = 0;

        {
            ref var current = ref MemoryMarshal.GetReference(dataArray.AsSpan());
            ref var limit = ref Unsafe.Add(ref current, dataArray.Length);

            while (Unsafe.IsAddressLessThan(ref current, ref limit))
            {
                MemoryMarshal.Write(buffer[index..], in current);
                index += tSize;
                current = ref Unsafe.Add(ref current, 1)!;
            }
        }

        {
            index = 0;
            ref var current = ref MemoryMarshal.GetReference(resultArray.AsSpan());
            ref var limit = ref Unsafe.Add(ref current, resultArray.Length);

            while (Unsafe.IsAddressLessThan(ref current, ref limit))
            {
                current = MemoryMarshal.Read<StructData>(buffer[index..]);
                index += tSize;
                current = ref Unsafe.Add(ref current, 1)!;
            }
        }

        Debug.Assert(dataArray.AsSpan().SequenceEqual(resultArray));
    }

    [Benchmark]
    public void AsBytes()
    {
        var buffer = GetBuffer();
        MemoryMarshal.AsBytes(dataArray.AsSpan()).CopyTo(buffer);
        MemoryMarshal.Cast<byte, StructData>(buffer).CopyTo(resultArray.AsSpan());
        Debug.Assert(dataArray.AsSpan().SequenceEqual(resultArray));
    }
}

public record struct StructData
{
    public int Field1;
    public uint Field2;
    public ulong Field3;
    public long Field4;
    public short Field5;
    public ushort Field6;
    public byte Field7;
    public sbyte Field8;
    public Int128 Field9;

    public static StructData Generate(Random random)
    {
        StructData result = new();
        random.NextBytes(Mem.AsBytes(ref result));
        return result;
    }

    public static StructData[] Generate(Random random, int count)
    {
        StructData[] result = new StructData[count];
        random.NextBytes(MemoryMarshal.AsBytes(result.AsSpan()));
        return result;
    }
}
