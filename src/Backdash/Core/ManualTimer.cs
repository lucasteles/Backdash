namespace Backdash.Core;

sealed class ManualTimer(IClock clock, TimeSpan interval, Action<TimeSpan> onTick)
{
    long lastRun = clock.GetTimeStamp();
    public long StartTime { get; } = clock.GetTimeStamp();

    public void Update()
    {
        if (interval == TimeSpan.Zero) return;
        var elapsed = clock.GetElapsedTime(lastRun);
        if (elapsed < interval)
            return;

        onTick(clock.GetElapsedTime(StartTime));
        Reset();
    }

    public void Reset() => lastRun = clock.GetTimeStamp();
}
