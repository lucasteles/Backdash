using System.Security.Cryptography;

namespace nGGPO.Utils;

static class MagicNumber
{
    public static ushort Generate()
    {
        using var gen = RandomNumberGenerator.Create();
        Span<byte> buff = stackalloc byte[sizeof(ushort)];
        gen.GetBytes(buff);
        return BitConverter.ToUInt16(buff);
    }
}
