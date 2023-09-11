using nGGPO.Types;

namespace nGGPO;

public static class GgpoExtensions
{
    public static bool IsSuccess(this ErrorCode code) => code is ErrorCode.Ok;
}