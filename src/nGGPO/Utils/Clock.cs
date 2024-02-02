using System.Diagnostics;

namespace nGGPO.Utils;

interface IClock
{
    long GetMilliseconds();
}

sealed class Clock : IClock
{
    public long GetTicks() => Stopwatch.GetTimestamp();

    public long GetMilliseconds() => GetTicks() / (Stopwatch.Frequency / 1000);
}
