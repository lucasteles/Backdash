using System.Runtime.CompilerServices;

namespace nGGPO;

static class Extensions
{
    public static bool IsSuccess(this ResultCode code) => code is ResultCode.Ok;
    public static bool IsFailure(this ResultCode code) => !code.IsSuccess();

    public static void AssertTrue(this bool value, [CallerMemberName] string? source = null)
    {
        if (!value)
            throw new NggpoException($"Invalid assertion at {source}");
    }

    public static uint NextUInt(this Random random) => (uint)random.Next() & 0xFFFF;

    public static double NextGaussian(this Random random)
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
