using System.Diagnostics;
namespace Backdash.Core;
interface IClock
{
    long GetTimeStamp();
    TimeSpan GetElapsedTime(long lastTimeStamp);
}
sealed class Clock : IClock
{
    public long GetTimeStamp() => Stopwatch.GetTimestamp();
    public TimeSpan GetElapsedTime(long lastTimeStamp) => Stopwatch.GetElapsedTime(lastTimeStamp);
}
