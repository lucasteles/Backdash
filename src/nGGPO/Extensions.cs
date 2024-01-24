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
}
