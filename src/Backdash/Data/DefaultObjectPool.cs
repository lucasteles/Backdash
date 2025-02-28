namespace Backdash.Data;

sealed class DefaultObjectPool<T> : IObjectPool<T> where T : class, new()
{
    public static readonly IObjectPool<T> Instance = new DefaultObjectPool<T>();

    readonly int maxCapacity; // -1 to account for fastItem

    int numItems;
    readonly Queue<T> items;
    readonly HashSet<T> set;
    T? fastItem;

    public DefaultObjectPool(int capacity = 100)
    {
        maxCapacity = capacity - 1;
        items = new(maxCapacity);
        set = new(maxCapacity);
    }

    public T Rent()
    {
        var item = fastItem;

        if (item is not null)
        {
            fastItem = null;
            return item;
        }

        if (!items.TryDequeue(out item))
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

        if (numItems >= maxCapacity)
            return;

        numItems++;
        items.Enqueue(value);
        set.Add(value);
    }
}
