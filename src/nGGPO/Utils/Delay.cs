namespace nGGPO.Utils;

static class Delay
{
    public const int OneFrameTime = 1_000 / 60;

    public static async ValueTask Of(long milliseconds, CancellationToken ct = default)
    {
        var threshold = TimeStamp.GetMilliseconds() + milliseconds;
        while (!ct.IsCancellationRequested && TimeStamp.GetMilliseconds() <= threshold)
            await Task.Yield();
    }
}
