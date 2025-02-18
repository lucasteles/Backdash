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
    uint Compute(ReadOnlySpan<byte> data);
}

/// <summary>
/// Provider always zero checksum
/// </summary>
public class EmptyChecksumProvider : IChecksumProvider
{
    /// <inheritdoc />
    public uint Compute(ReadOnlySpan<byte> data) => 0;
}

/// <summary>
/// Fletcher 32 checksum provider
/// see: http://en.wikipedia.org/wiki/Fletcher%27s_checksum
/// </summary>
public sealed class Fletcher32ChecksumProvider : IChecksumProvider
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
