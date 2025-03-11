// ReSharper disable UnassignedField.Global

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Backdash.Network;
using Backdash.Serialization;
using Backdash.Serialization.Internal;

namespace Backdash.Benchmarks.Cases;

[RPlotExporter]
[InProcess, MemoryDiagnoser]
public class IntSerializerBenchmark
{
    int[] data = [];

    [Params(10, 100, 1_000, 10_000, 100_000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        Random random = new(42);
        data = new int[N];
        random.NextBytes(MemoryMarshal.AsBytes(data.AsSpan()));
    }

    static readonly IntegerBinaryLittleEndianSerializer<int> cache = new(false);
    static readonly RawIntegerBinaryLittleEndianSerializer<int> raw = new(false);
    static readonly UnsafeIntegerBinaryLittleEndianSerializer<int> @unsafe = new(false);
    static readonly ByteCountIntegerBinaryLittleEndianSerializer<int> count = new(false);

    public void Run(IBinarySerializer<int> serializer)
    {
        Span<byte> buffer = stackalloc byte[4];
        var result = 0;

        for (var i = 0; i < data.Length; i++)
        {
            ref var curr = ref data[i];
            serializer.Serialize(in curr, buffer);
            serializer.Deserialize(buffer, ref result);
            Debug.Assert(curr == result);
        }
    }

    [Benchmark]
    public void Cached() => Run(cache);

    [Benchmark]
    public void Raw() => Run(raw);

    [Benchmark]
    public void Unsafe() => Run(@unsafe);

    [Benchmark]
    public void ByteCount() => Run(count);
}

sealed class RawIntegerBinaryLittleEndianSerializer<T>(bool isUnsigned)
    : IBinarySerializer<T> where T : unmanaged, IBinaryInteger<T>
{
    public Endianness Endianness => Endianness.LittleEndian;

    public int Serialize(in T data, Span<byte> buffer)
    {
        Unsafe.AsRef(in data).TryWriteLittleEndian(buffer, out var size);
        return size;
    }

    public int Deserialize(ReadOnlySpan<byte> data, ref T value)
    {
        var size = Unsafe.SizeOf<T>();
        value = T.ReadLittleEndian(data[..size], isUnsigned);
        return size;
    }
}

sealed unsafe class UnsafeIntegerBinaryLittleEndianSerializer<T>(bool isUnsigned)
    : IBinarySerializer<T> where T : unmanaged, IBinaryInteger<T>
{
    public Endianness Endianness => Endianness.LittleEndian;

    public int Serialize(in T data, Span<byte> buffer)
    {
        Unsafe.AsRef(in data).TryWriteLittleEndian(buffer, out var size);
        return size;
    }

    public int Deserialize(ReadOnlySpan<byte> data, ref T value)
    {
        var size = sizeof(T);
        value = T.ReadLittleEndian(data[..size], isUnsigned);
        return size;
    }
}

sealed class ByteCountIntegerBinaryLittleEndianSerializer<T>(bool isUnsigned)
    : IBinarySerializer<T> where T : unmanaged, IBinaryInteger<T>
{
    public Endianness Endianness => Endianness.LittleEndian;

    public int Serialize(in T data, Span<byte> buffer)
    {
        Unsafe.AsRef(in data).TryWriteLittleEndian(buffer, out var size);
        return size;
    }

    public int Deserialize(ReadOnlySpan<byte> data, ref T value)
    {
        var size = value.GetByteCount();
        value = T.ReadLittleEndian(data[..size], isUnsigned);
        return size;
    }
}
