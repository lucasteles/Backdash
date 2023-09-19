using System;
using System.Security.Cryptography;

namespace nGGPO.Utils;

public static class Rnd
{
    public static readonly Random Shared = new();

    public static ushort MagicNumber()
    {
        using var gen = RandomNumberGenerator.Create();
        Span<byte> buff = stackalloc byte[sizeof(ushort)];
        gen.GetBytes(buff);
        return BitConverter.ToUInt16(buff);
    }

    public static uint NextUInt(this Random random) => (uint) random.Next() & 0xFFFF;
}