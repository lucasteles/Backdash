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

/// <summary>
/// Provider always zero checksum
/// </summary>
public class EmptyChecksumProvider : IChecksumProvider
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
