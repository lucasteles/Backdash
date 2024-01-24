using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace nGGPO.Utils;

static class TimeStamp
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetTicks() => Stopwatch.GetTimestamp();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetMilliseconds() => GetTicks() / (Stopwatch.Frequency / 1000);
}
