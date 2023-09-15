using System;
using System.Security.Cryptography;

namespace nGGPO.Utils;

public static class Magic
{
    public static ushort Number()
    {
        using var gen = RandomNumberGenerator.Create();
        Span<byte> buff = stackalloc byte[sizeof(ushort)];
        gen.GetBytes(buff);
        return BitConverter.ToUInt16(buff);
    }
}