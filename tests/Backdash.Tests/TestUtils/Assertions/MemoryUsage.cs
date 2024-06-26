namespace Backdash.Tests.TestUtils.Assertions;

public sealed class MemoryUsage : IDisposable
{
    readonly long startingMemory = GC.GetTotalMemory(true) / 1024;
    readonly List<long> checkPoints = [];

    public void CheckPoint()
    {
        var point = GC.GetTotalMemory(false) / 1024;
        checkPoints.Add(point - startingMemory);
    }

    public void Dispose()
    {
        CheckPoint();
        var allocated = checkPoints[^1];
        Console.WriteLine(allocated);
    }
}
