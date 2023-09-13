using nGGPO.Types;

namespace nGGPO;

public static class Extensions
{
    public static bool IsSuccess(this ErrorCode code) => code is ErrorCode.Ok;
}