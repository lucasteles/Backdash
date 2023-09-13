using System;
using System.Diagnostics;

namespace nGGPO.Types;

public static class Platform
{
    public static long GetCurrentTimeMS() => Stopwatch.GetTimestamp() / 10_000;

    public static int GetConfigInt(string name) =>
        int.TryParse(Environment.GetEnvironmentVariable(name), out var value) ? value : default;
}