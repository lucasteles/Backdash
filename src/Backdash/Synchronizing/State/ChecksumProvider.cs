using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Backdash.Synchronizing.State;

/// <summary>
/// Provider of checksum values
/// </summary>
public interface IChecksumProvider
{
    /// <summary>
    /// Returns the checksum value for <paramref name="data"/>.
    /// </summary>
    /// <param name="data"></param>
    /// <returns><see cref="int"/> checksum value</returns>
    int Compute(ReadOnlySpan<byte> data);
}

/// <inheritdoc />
sealed class EmptyChecksumProvider : IChecksumProvider
{
    /// <inheritdoc />
    public int Compute(ReadOnlySpan<byte> data) => 0;
}

/// <summary>
/// Fletcher 32 checksum provider
/// see: http://en.wikipedia.org/wiki/Fletcher%27s_checksum
/// </summary>
public sealed class Fletcher32ChecksumProvider : IChecksumProvider
{
    /// <inheritdoc />
    public int Compute(ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty) return 0;
        var buffer = MemoryMarshal.Cast<byte, short>(data);
        int sum1 = 0xFFFF, sum2 = 0xFFFF;
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

        sum1 = (sum1 & 0xFFFF) + (sum1 >> 16);
        sum2 = (sum2 & 0xFFFF) + (sum2 >> 16);
        return (sum2 << 16) | sum1;
    }
}

/// <summary>
/// CRC32 (Cyclic Redundancy Check) checksum provider
/// see: https://en.wikipedia.org/wiki/Cyclic_redundancy_check
/// </summary>
public sealed class Crc32ChecksumProvider : IChecksumProvider
{
    static uint[] CreateTable()
    {
        const uint polynomial = 0xEDB88320;

        var tableValues = new uint[256];
        for (uint i = 0; i < tableValues.Length; i++)
        {
            var crc = i;
            for (var j = 0; j < 8; j++)
            {
                if ((crc & 1) is not 0)
                    crc = (crc >> 1) ^ polynomial;
                else
                    crc >>= 1;
            }

            tableValues[i] = crc;
        }

        return tableValues;
    }

    static readonly uint[] table = CreateTable();

    /// <inheritdoc />
    public int Compute(ReadOnlySpan<byte> data)
    {
        var crc = 0xFFFFFFFF;
        ref var current = ref MemoryMarshal.GetReference(data);
        ref readonly var limit = ref Unsafe.Add(ref current, data.Length);

        while (Unsafe.IsAddressLessThan(in current, in limit))
        {
            var index = (byte)((crc & 0xFF) ^ current);
            crc = (crc >> 8) ^ table[index];

            current = ref Unsafe.Add(ref current, 1);
        }

        return (int)~crc;
    }
}
