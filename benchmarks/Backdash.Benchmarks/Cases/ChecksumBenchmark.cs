// ReSharper disable UnassignedField.Global

using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Backdash.Synchronizing.State;

namespace Backdash.Benchmarks.Cases;

[RPlotExporter]
[InProcess, MemoryDiagnoser]
public class ChecksumBenchmark
{
    byte[] data = [];

    [Params(10, 100, 1_000, 10_000, 100_000)]
    public int N;

    [GlobalSetup]
    public void Setup()
    {
        Random random = new(42);
        data = new byte[N];
        random.NextBytes(data);
    }

    static readonly Fletcher32SpanChecksumProvider fletcher32Span = new();
    static readonly Fletcher32UnsafeChecksumProvider fletcher32Unsafe = new();
    static readonly Crc32ChecksumProvider crc32 = new();
    static readonly Crc32BigEndianChecksumProvider crc32BigEndian = new();

    [Benchmark]
    public uint Fletcher32Span() => fletcher32Span.Compute(data);

    [Benchmark]
    public uint Fletcher32Unsafe() => fletcher32Unsafe.Compute(data);

    [Benchmark]
    public uint Crc32() => crc32.Compute(data);

    [Benchmark]
    public uint Crc32_BigEndian() => crc32BigEndian.Compute(data);
}

public sealed class Fletcher32SpanChecksumProvider : IChecksumProvider
{
    /// <inheritdoc />
    public uint Compute(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty) return 0;
        var buffer = MemoryMarshal.Cast<byte, ushort>(data);
        uint sum1 = 0xFFFF, sum2 = 0xFFFF;
        var dataIndex = 0;
        var len = buffer.Length;

        while (len > 0)
        {
            var tLen = len > 360 ? 360 : len;
            len -= tLen;

            do
            {
                sum1 += buffer[dataIndex++];
                sum2 += sum1;
            } while (--tLen > 0);

            sum1 = (sum1 & 0xFFFF) + (sum1 >> 16);
            sum2 = (sum2 & 0xFFFF) + (sum2 >> 16);
        }

        if ((data.Length & 1) is 1)
        {
            sum1 += data[^1];
            sum2 += sum1;
        }

        sum1 = (sum1 & 0xFFFF) + (sum1 >> 16);
        sum2 = (sum2 & 0xFFFF) + (sum2 >> 16);
        return (sum2 << 16) | sum1;
    }
}

public sealed class Fletcher32UnsafeChecksumProvider : IChecksumProvider
{
    const int BlockSize = 360;

    /// <inheritdoc />
    public unsafe uint Compute(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty) return 0;

        uint sum1 = 0xFFFF, sum2 = 0xFFFF;
        var dataIndex = 0;
        var dataLen = data.Length;
        var len = dataLen / sizeof(ushort);

        fixed (byte* ptr = data)
        {
            while (len > 0)
            {
                var blockLen = len > BlockSize ? BlockSize : len;
                len -= blockLen;

                do
                {
                    sum1 += *(ushort*)(ptr + dataIndex);
                    sum2 += sum1;
                    dataIndex += sizeof(ushort);
                } while (--blockLen > 0);

                sum1 = (sum1 & 0xFFFF) + (sum1 >> 16);
                sum2 = (sum2 & 0xFFFF) + (sum2 >> 16);
            }

            // if ((dataLen & 1) == 1)
            if (dataIndex < dataLen)
            {
                sum1 += *(ptr + dataLen - 1);
                sum2 += sum1;
            }
        }

        sum1 = (sum1 & 0xFFFF) + (sum1 >> 16);
        sum2 = (sum2 & 0xFFFF) + (sum2 >> 16);
        return (sum2 << 16) | sum1;
    }
}

public sealed class Crc32ChecksumProvider : IChecksumProvider
{
    /// <inheritdoc />
    public uint Compute(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0) return 0;

        uint sum0 = 0, sum1 = 0, sum2 = 0, sum3 = 0;

        for (var i = 0; i < data.Length; i++)
        {
            switch (i % 4)
            {
                case 0: sum0 += data[i]; break;
                case 1: sum1 += data[i]; break;
                case 2: sum2 += data[i]; break;
                case 3: sum3 += data[i]; break;
            }
        }

        var sum = sum3 + (sum2 << 8) + (sum1 << 16) + (sum0 << 24);

        return sum;
    }
}

public sealed class Crc32BigEndianChecksumProvider : IChecksumProvider
{
    /// <inheritdoc />
    public unsafe uint Compute(ReadOnlySpan<byte> data)
    {
        if (data.Length == 0) return 0;

        fixed (byte* ptr = data)
        {
            uint sum = 0;
            int z = 0;

            var limit = data.Length - 32;
            while (z <= limit)
            {
                sum += BinaryPrimitives.ReverseEndianness(*(uint*)(ptr + z));
                sum += BinaryPrimitives.ReverseEndianness(*(uint*)(ptr + z + 4));
                sum += BinaryPrimitives.ReverseEndianness(*(uint*)(ptr + z + 8));
                sum += BinaryPrimitives.ReverseEndianness(*(uint*)(ptr + z + 12));
                sum += BinaryPrimitives.ReverseEndianness(*(uint*)(ptr + z + 16));
                sum += BinaryPrimitives.ReverseEndianness(*(uint*)(ptr + z + 20));
                sum += BinaryPrimitives.ReverseEndianness(*(uint*)(ptr + z + 24));
                sum += BinaryPrimitives.ReverseEndianness(*(uint*)(ptr + z + 28));

                z += 32;
            }

            limit = data.Length - 4;
            while (z <= limit)
            {
                sum += BinaryPrimitives.ReverseEndianness(*(uint*)(ptr + z));
                z += 4;
            }

            int rem = data.Length - z;

            switch (rem & 3)
            {
                case 3:
                    sum += (uint)*(ptr + z + 2) << 8;
                    sum += (uint)*(ptr + z + 1) << 16;
                    sum += (uint)*(ptr + z) << 24;
                    break;
                case 2:
                    sum += (uint)*(ptr + z + 1) << 16;
                    sum += (uint)*(ptr + z) << 24;
                    break;
                case 1:
                    sum += (uint)*(ptr + z) << 24;
                    break;
            }

            return sum;
        }
    }
}
