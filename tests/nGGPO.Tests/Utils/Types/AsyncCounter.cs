namespace nGGPO.Tests.Utils;

public sealed class AsyncCounter
{
    int currentCount = 0;
    public int Value => currentCount;
    public void Inc() => Interlocked.Increment(ref currentCount);
    public void Dec() => Interlocked.Decrement(ref currentCount);
    public void Reset() => currentCount = 0;
}
