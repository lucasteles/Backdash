namespace Backdash.Synchronizing.State;

/// <summary>
/// Provider of checksum values for <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">Game state type</typeparam>
public interface IChecksumProvider<T> where T : notnull
{
    /// <summary>
    /// Returns the checksum value for <paramref name="value"/>.
    /// </summary>
    /// <param name="value"></param>
    /// <returns><see cref="int"/> checksum value</returns>
    int Compute(in T value);
}

/// <summary>
/// HashCode checksum provider for <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">Game state type</typeparam>
public sealed class HashCodeChecksumProvider<T> : IChecksumProvider<T> where T : notnull
{
    /// <inheritdoc />
    public int Compute(in T value) => EqualityComparer<T>.Default.GetHashCode(value);
}

/// <inheritdoc />
sealed class EmptyChecksumProvider<T> : IChecksumProvider<T> where T : notnull
{
    /// <inheritdoc />
    public int Compute(in T value) => 0;
}

static class ChecksumProviderFactory
{
    public static IChecksumProvider<T> Create<T>() where T : notnull
    {
#if !AOT_ENABLED
        if (Core.TypeHelpers.HasInvariantHashCode<T>())
            return new HashCodeChecksumProvider<T>();
#endif

        return new EmptyChecksumProvider<T>();
    }
}
