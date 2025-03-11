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

    [Params(
        1, 5, 10, 100, 1000, 10_000,
        2, 4, 8, 16, 32, 64, 256, 1024, 8192
    )]
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

    [Benchmark]
    public int NextReference() => PopCount.NextReference<byte>(data);

    [Benchmark]
    public int TypeSize() => PopCount.TypeSize<byte>(data);
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

    public static int NextReference<T>(in ReadOnlySpan<T> values) where T : unmanaged
    {
        var count = 0;
        var bytes = MemoryMarshal.AsBytes(values);
        ref var current = ref MemoryMarshal.GetReference(bytes);
        ref var limit = ref Unsafe.Add(ref current, bytes.Length);
        ref var next = ref current;

#pragma warning disable S907
        while (Unsafe.IsAddressLessThan(ref current, ref limit))
        {
            next = ref Unsafe.Add(ref current, sizeof(ulong) - 1);
            if (Unsafe.IsAddressLessThan(ref next, ref limit))
            {
                var value = Unsafe.ReadUnaligned<ulong>(ref current);
                count += BitOperations.PopCount(value);
                current = ref next;
                goto LOOP;
            }

            next = ref Unsafe.Add(ref current, sizeof(uint) - 1);
            if (Unsafe.IsAddressLessThan(ref next, ref limit))
            {
                var value = Unsafe.ReadUnaligned<uint>(ref current);
                count += BitOperations.PopCount(value);
                current = ref next;
                goto LOOP;
            }

            next = ref Unsafe.Add(ref current, sizeof(ushort) - 1);
            if (Unsafe.IsAddressLessThan(ref next, ref limit))
            {
                var value = Unsafe.ReadUnaligned<ushort>(ref current);
                count += ushort.PopCount(value);
                current = ref next;
                goto LOOP;
            }

            count += byte.PopCount(current);

LOOP:
            current = ref Unsafe.Add(ref current, 1);
        }
#pragma warning restore S907

        return count;
    }

    public static int TypeSize<T>(in ReadOnlySpan<T> values) where T : unmanaged
    {
        var size = Unsafe.SizeOf<T>();

        if (size % sizeof(ulong) is 0)
            return NumType(MemoryMarshal.Cast<T, ulong>(values));

        if (size % sizeof(uint) is 0)
            return NumType(MemoryMarshal.Cast<T, uint>(values));

        if (size % sizeof(ushort) is 0)
            return NumType(MemoryMarshal.Cast<T, ushort>(values));

        return NumType(MemoryMarshal.AsBytes(values));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NumType(in ReadOnlySpan<uint> values)
    {
        var count = 0;
        ref var current = ref MemoryMarshal.GetReference(values);
        ref var limit = ref Unsafe.Add(ref current, values.Length);
        while (Unsafe.IsAddressLessThan(ref current, ref limit))
        {
            count += BitOperations.PopCount(current);
            current = ref Unsafe.Add(ref current, 1);
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NumType(in ReadOnlySpan<ulong> values)
    {
        var count = 0;
        ref var current = ref MemoryMarshal.GetReference(values);
        ref var limit = ref Unsafe.Add(ref current, values.Length);
        while (Unsafe.IsAddressLessThan(ref current, ref limit))
        {
            count += BitOperations.PopCount(current);
            current = ref Unsafe.Add(ref current, 1);
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NumType(in ReadOnlySpan<ushort> values)
    {
        var count = 0;
        ref var current = ref MemoryMarshal.GetReference(values);
        ref var limit = ref Unsafe.Add(ref current, values.Length);
        while (Unsafe.IsAddressLessThan(ref current, ref limit))
        {
            count += BitOperations.PopCount(current);
            current = ref Unsafe.Add(ref current, 1);
        }

        return count;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int NumType(in ReadOnlySpan<byte> values)
    {
        var count = 0;
        ref var current = ref MemoryMarshal.GetReference(values);
        ref var limit = ref Unsafe.Add(ref current, values.Length);
        while (Unsafe.IsAddressLessThan(ref current, ref limit))
        {
            count += BitOperations.PopCount(current);
            current = ref Unsafe.Add(ref current, 1);
        }

        return count;
    }
}
