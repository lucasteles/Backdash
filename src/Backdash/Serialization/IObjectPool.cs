namespace Backdash.Serialization;

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

sealed class NewFakePool<T> : IObjectPool<T> where T : class, new()
{
    public static readonly IObjectPool<T> Instance = new NewFakePool<T>();

    public T Rent() => new();
    public void Return(T value) { }
}
