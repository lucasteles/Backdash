using System;
using System.Runtime.CompilerServices;
using nGGPO.Types;

namespace nGGPO;

public static class Extensions
{
    public static bool IsSuccess(this ErrorCode code) => code is ErrorCode.Ok;

    public static void AssertTrue(this bool value, [CallerMemberName] string? source = null)
    {
        if (!value)
            throw new InvalidOperationException($"Invalid assertion at {source}");
    }
}