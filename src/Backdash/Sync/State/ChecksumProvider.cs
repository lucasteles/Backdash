using Backdash.Core;

namespace Backdash.Sync.State;

public interface IChecksumProvider<T> where T : notnull
{
    int Compute(in T value);
}

public sealed class HashCodeChecksumProvider<T> : IChecksumProvider<T> where T : notnull
{
    public int Compute(in T value) => EqualityComparer<T>.Default.GetHashCode(value);
}

public sealed class EmptyChecksumProvider<T> : IChecksumProvider<T> where T : notnull
{
    public int Compute(in T value) => 0;
}

static class ChecksumProviderFactory
{
    public static IChecksumProvider<T> Create<T>() where T : notnull
    {
        if (TypeHelpers.HasInvariantHashCode<T>())
            return new HashCodeChecksumProvider<T>();

        return new EmptyChecksumProvider<T>();
    }
}
