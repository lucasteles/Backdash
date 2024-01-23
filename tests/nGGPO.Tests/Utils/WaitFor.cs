namespace nGGPO.Tests.Utils;

public static class WaitFor
{
    public static async Task BeTrue(Func<bool> checkTask,
        TimeSpan timeout, TimeSpan? next = null
    )
    {
        next ??= TimeSpan.FromSeconds(1.0 / 60);

        async Task WaitLoop()
        {
            while (!checkTask())
                await Task.Delay(next.Value);
        }

        await WaitLoop().WaitAsync(timeout);
    }

    public static Task BeTrue(Func<bool> checkTask) =>
        BeTrue(checkTask, TimeSpan.FromSeconds(1));
}
