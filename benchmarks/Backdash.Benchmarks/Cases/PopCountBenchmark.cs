// ReSharper disable UnassignedField.Global

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Backdash.Benchmarks.Cases;

[RPlotExporter]
[InProcess, MemoryDiagnoser]
public class PopCountBenchmark
{
    byte[] data = [];

    [Params(10, 100, 1000, 10_000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        Random random = new(42);
        data = new byte[N];
        random.NextBytes(data);
    }

    [Benchmark]
    public int WhileSize() => PopCount.WhileSize<byte>(data);
}

static class PopCount
{
    public static int WhileSize<T>(in ReadOnlySpan<T> values) where T : unmanaged
    {
        var bytes = MemoryMarshal.AsBytes(values);
        var index = 0;
        var count = 0;

        while (index < bytes.Length)
        {
            var remaining = bytes[index..];

            switch (remaining.Length)
            {
                case >= sizeof(ulong):
                {
                    var value = MemoryMarshal.Read<ulong>(remaining[..sizeof(ulong)]);
                    index += sizeof(ulong);
                    count += BitOperations.PopCount(value);
                    continue;
                }
                case >= sizeof(uint):
                {
                    var value = MemoryMarshal.Read<uint>(remaining[..sizeof(uint)]);
                    index += sizeof(uint);
                    count += BitOperations.PopCount(value);
                    continue;
                }
                case >= sizeof(ushort):
                {
                    var value = MemoryMarshal.Read<ushort>(remaining[..sizeof(ushort)]);
                    index += sizeof(ushort);
                    count += ushort.PopCount(value);
                    continue;
                }
                case >= sizeof(byte):
                {
                    var value = remaining[0];
                    index += sizeof(byte);
                    count += byte.PopCount(value);
                    break;
                }
            }
        }

        return count;
    }

    public static int RefSize<T>(in ReadOnlySpan<T> values) where T : unmanaged
    {
        var count = 0;

        var bytes = MemoryMarshal.AsBytes(values);
        ref var current = ref MemoryMarshal.GetReference(bytes);
        ref var limit = ref Unsafe.Add(ref current, bytes.Length);
        ref var next = ref current;

        while (Unsafe.IsAddressLessThan(ref current, ref limit))
        {
            next = ref Unsafe.Add(ref current, sizeof(ulong));
            if (Unsafe.IsAddressLessThan(ref next, ref limit))
            {
                var value = Unsafe.ReadUnaligned<ulong>(ref current);
                count += BitOperations.PopCount(value);
                current = ref next;
                continue;
            }

            next = ref Unsafe.Add(ref current, sizeof(uint));
            if (Unsafe.IsAddressLessThan(ref next, ref limit))
            {
                var value = Unsafe.ReadUnaligned<uint>(ref current);
                count += BitOperations.PopCount(value);
                current = ref next;
                continue;
            }

            next = ref Unsafe.Add(ref current, sizeof(ushort));
            if (Unsafe.IsAddressLessThan(ref next, ref limit))
            {
                var value = Unsafe.ReadUnaligned<ushort>(ref current);
                count += ushort.PopCount(value);
                current = ref next;
                continue;
            }

            next = ref Unsafe.Add(ref current, sizeof(byte));
            if (Unsafe.IsAddressLessThan(ref next, ref limit))
            {
                count += byte.PopCount(current);
                current = ref Unsafe.Add(ref current, sizeof(byte))!;
                current = ref next;
                continue;
            }

            current = ref Unsafe.Add(ref current, 1)!;
        }

        return count;
    }
}
