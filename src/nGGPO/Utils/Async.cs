namespace nGGPO.Utils;

static class Async
{
    const int FrameTime = 1_000 / 60;

    public static async ValueTask Delay(long milliseconds, CancellationToken ct = default)
    {
        var threshold = TimeStamp.GetMilliseconds() + milliseconds;
        while (!ct.IsCancellationRequested && TimeStamp.GetMilliseconds() <= threshold)
            await Task.Yield();
    }

    public static ValueTask Delay(TimeSpan duration, CancellationToken ct = default) =>
        Delay((long)duration.TotalMilliseconds, ct);

    public static ValueTask OneFrameDelay(CancellationToken ct = default) => Delay(FrameTime, ct);
}
