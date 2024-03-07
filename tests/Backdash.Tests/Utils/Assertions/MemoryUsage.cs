namespace Backdash.Tests.Utils.Assertions;
public sealed class MemoryUsage : IDisposable
{
    readonly long startingMemory = GC.GetTotalMemory(true) / 1024;
    readonly List<long> checkPoints = new();
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
