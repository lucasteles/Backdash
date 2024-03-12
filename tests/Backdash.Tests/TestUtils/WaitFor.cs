using System.Runtime.CompilerServices;

namespace Backdash.Tests.TestUtils;
public static class WaitFor
{
    public static async Task BeTrue(
        Func<bool> checkTask,
        TimeSpan? timeout = null,
        string? because = null,
        TimeSpan? next = null,
        [CallerArgumentExpression(nameof(checkTask))]
        string? source = null
    )
    {
        timeout ??= TimeSpan.FromSeconds(5);
        next ??= TimeSpan.FromSeconds(1.0 / 60);
        async Task WaitLoop()
        {
            while (!checkTask())
                await Task.Delay(next.Value);
        }
        try
        {
            await WaitLoop().WaitAsync(timeout.Value);
        }
        catch (TimeoutException)
        {
            because = because is null ? string.Empty : $", {because}";
            Assert.Fail($"Timeout waiting for {source}{because}");
            throw;
        }
    }
}
