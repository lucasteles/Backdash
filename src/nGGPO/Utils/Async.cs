namespace nGGPO.Utils;

static class Async
{
    const int FrameTime = 1_000 / 60;

    public static Task OneFrameDelay(CancellationToken ct = default) => Task.Delay(FrameTime, ct);
}
