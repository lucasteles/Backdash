#pragma warning disable S1854

// ReSharper disable UnassignedField.Global
using System.Numerics;
using Backdash.Core;

namespace Backdash.Benchmarks.Cases;

[RPlotExporter]
[InProcess, MemoryDiagnoser]
public class MemBenchmark
{
    [Params(0, 1, 2, 3, 4)]
    public int Index;

    byte[][] bytes1 = [];
    byte[][] bytes2 = [];
    byte[][] results = [];

    [GlobalSetup]
    public void Setup()
    {
        var count = Vector<byte>.Count;
        Random random = new(42);

        bytes1 =
        [
            new byte[1],
            new byte[count - 1],
            new byte[count + 1],
            new byte[count * 2],
            new byte[count * 10],
        ];

        bytes2 =
        [
            new byte[1],
            new byte[count - 1],
            new byte[count + 1],
            new byte[count * 2],
            new byte[count * 10],
        ];

        results =
        [
            new byte[1],
            new byte[count - 1],
            new byte[count + 1],
            new byte[count * 2],
            new byte[count * 10],
        ];

        foreach (var data in bytes1.Concat(bytes2))
            random.NextBytes(data);
    }

    [Benchmark(Baseline = true)]
    public void XorSerial() => XorSerial(bytes1[Index], bytes2[Index], results[Index]);

    [Benchmark]
    public void XorSimd() => Mem.Xor(bytes1[Index], bytes2[Index], results[Index]);

    public static int XorSerial(ReadOnlySpan<byte> value1, ReadOnlySpan<byte> value2,
        Span<byte> result)
    {
        if (value1.Length != value2.Length)
            throw new ArgumentException(
                $"{nameof(value1)} and {nameof(value2)} must have same size");

        if (result.Length < value2.Length)
            throw new ArgumentException($"{nameof(result)} is too short");

        for (var i = 0; i < value1.Length; i++)
            result[i] = (byte)(value1[i] ^ value2[i]);

        return value1.Length;
    }
}