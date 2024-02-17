namespace Backdash.Sync.State;

public interface IChecksumProvider<T> where T : notnull
{
    int Compute(in T value);
}

public sealed class DefaultChecksumProvider<T> : IChecksumProvider<T> where T : notnull
{
    public int Compute(in T value) => EqualityComparer<T>.Default.GetHashCode(value);
}
