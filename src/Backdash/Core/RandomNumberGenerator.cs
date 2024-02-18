using System.Security.Cryptography;

namespace Backdash.Core;

interface IRandomNumberGenerator
{
    uint SyncNumber();
    ushort MagicNumber();
    int NextInt();
    double NextGaussian();
}

sealed class DefaultRandomNumberGenerator(Random random) : IRandomNumberGenerator
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

    public double NextGaussian()
    {
        double u, v, s;
        do
        {
            u = (2.0 * random.NextDouble()) - 1.0;
            v = (2.0 * random.NextDouble()) - 1.0;
            s = (u * u) + (v * v);
        } while (s >= 1.0);

        var fac = Math.Sqrt(-2.0 * Math.Log(s) / s);
        return u * fac;
    }
}
