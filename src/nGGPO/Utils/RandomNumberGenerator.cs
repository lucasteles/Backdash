using System.Security.Cryptography;

namespace nGGPO.Utils;

interface IRandomNumberGenerator
{
    uint SyncNumber();
    ushort MagicNumber();
    int NextInt();
}

sealed class CryptographyRandomNumberGenerator(Random random) : IRandomNumberGenerator
{
    public uint SyncNumber()
    {
        using var gen = RandomNumberGenerator.Create();
        Span<byte> buff = stackalloc byte[sizeof(uint)];
        gen.GetBytes(buff);
        return BitConverter.ToUInt32(buff);
    }

    public ushort MagicNumber()
    {
        using var gen = RandomNumberGenerator.Create();
        Span<byte> buff = stackalloc byte[sizeof(ushort)];
        gen.GetBytes(buff);
        return BitConverter.ToUInt16(buff);
    }

    public int NextInt() => random.Next();
}
