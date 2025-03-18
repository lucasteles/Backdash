namespace Backdash.Data;

/// <summary>
///     Defines an object pooling contract
/// </summary>
/// <typeparam name="T"></typeparam>
public interface IObjectPool<T>
{
    /// <summary>
    ///     Rent an instance on <typeparamref name="T" /> from the pool
    /// </summary>
    T Rent();

    /// <summary>
    ///     Return <paramref name="value" /> to the pool
    /// </summary>
    void Return(T value);
}

/// <summary>
///     Default object pool for types with empty constructor
/// </summary>
public sealed class DefaultObjectPool<T> : IObjectPool<T> where T : class, new()
{
    /// <summary>
    ///     Default object pool singleton.
    /// </summary>
    public static readonly IObjectPool<T> Instance = new DefaultObjectPool<T>();

    /// <summary>
    ///     Maximum number of objects allowed in the pool
    /// </summary>
    public readonly int MaxCapacity; // -1 to account for fastItem

    int numItems;
    readonly Stack<T> items;
    readonly HashSet<T> set;
    T? fastItem;

    /// <summary>
    ///     Instantiate new <see cref="DefaultObjectPool{T}" />
    /// </summary>
    public DefaultObjectPool(int capacity = 100, IEqualityComparer<T>? comparer = null)
    {
        MaxCapacity = capacity - 1;
        items = new(MaxCapacity);
        set = new(MaxCapacity, comparer ?? ReferenceEqualityComparer.Instance);
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
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

    /// <summary>
    ///     Clear object pool
    /// </summary>
    public void Clear()
    {
        numItems = 0;
        fastItem = null;
        items.Clear();
        set.Clear();
    }

    /// <summary>
    ///     Number of instances in the object pool
    /// </summary>
    public int Count => numItems + (fastItem is null ? 0 : 1);
}
