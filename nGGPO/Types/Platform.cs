using System.Diagnostics;

namespace nGGPO.Types;

public static class Platform
{
    public static long GetCurrentTimeMS() => Stopwatch.GetTimestamp() / 10_000;
}