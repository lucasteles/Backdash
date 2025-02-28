namespace Backdash.Data;

/// <summary>
/// Defines a object pooling contract
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IObjectPool<T>
{
    /// <summary>
    /// Rent an instance on <typeparamref name="T"/> from the pool
    /// </summary>
    T Rent();

    /// <summary>
    /// Return <paramref name="value"/> to the pool
    /// </summary>
    void Return(T value);
}
