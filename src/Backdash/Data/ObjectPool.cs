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

sealed class DefaultObjectPool<T> : IObjectPool<T> where T : class, new()
{
    public static readonly IObjectPool<T> Instance = new DefaultObjectPool<T>();

    const int MaxCapacity = 99; // -1 to account for fastItem

    int numItems;
    readonly Stack<T> items = new(MaxCapacity);
    readonly HashSet<T> set = new(MaxCapacity, ReferenceEqualityComparer.Instance);
    T? fastItem;

    public T Rent()
    {
        var item = fastItem;

        if (item is not null)
        {
            fastItem = null;
            return item;
        }

        if (!items.TryPop(out item))
            return new();

        numItems--;
        set.Remove(item);
        return item;
    }

    public void Return(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (ReferenceEquals(fastItem, value) || set.Contains(value))
            return;

        if (fastItem is null)
        {
            fastItem = value;
            return;
        }

        if (numItems >= MaxCapacity)
            return;

        numItems++;
        items.Push(value);
        set.Add(value);
    }

    public void Clear()
    {
        numItems = 0;
        fastItem = null;
        items.Clear();
        set.Clear();
    }

    public int Count => numItems + (fastItem is null ? 0 : 1);
}
