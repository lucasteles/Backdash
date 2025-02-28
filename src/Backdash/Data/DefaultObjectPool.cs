namespace Backdash.Data;

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
}
