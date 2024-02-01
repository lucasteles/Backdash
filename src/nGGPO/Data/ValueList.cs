using System.Collections;
using System.Runtime.CompilerServices;
using nGGPO.Utils;

namespace nGGPO.Data;

struct ValueList<T> : IList<T>, IEquatable<ValueList<T>> where T : struct, IEquatable<T>
{
    public const int MaxSize = 32;

    ValueListBuffer buffer;

    public int Count { get; private set; } = 0;

    public ValueList()
    {
        if (!Mem.IsValidSizeOnStack<ValueList<T>>())
            throw new NggpoException($"{typeof(T).Name} is too big for stack");
    }

    public readonly bool IsReadOnly => false;

    public readonly bool IsFull() => Count >= MaxSize;

    public void Add(in T task)
    {
        if (IsFull()) throw new NggpoException("ValueTaskList is full");
        buffer[Count++] = task;
    }

    void ICollection<T>.Add(T item) => Add(in item);

    public void Clear()
    {
        Span<T> span = buffer;
        span.Clear();
        Count = 0;
    }

    public readonly bool Contains(T item) => buffer[..Count].Contains(item);

    public readonly void CopyTo(T[] array, int arrayIndex) =>
        buffer[..Count].CopyTo(array.AsSpan()[arrayIndex..]);

    public readonly int IndexOf(T item)
    {
        ReadOnlySpan<T> span = buffer;
        return span.IndexOf(item);
    }

    public void Insert(int index, T item)
    {
        if (index < 0 || index >= Count)
            throw new ArgumentOutOfRangeException(nameof(index));
        if (IsFull()) throw new NggpoException("ValueTaskList is full");

        Span<T> span = buffer;
        span[index..Count].CopyTo(span[(index + 1)..(Count + 1)]);
        span[index] = item;
        Count++;
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= Count)
            throw new ArgumentOutOfRangeException(nameof(index));

        Span<T> span = buffer;
        span[(index + 1)..Count].CopyTo(span[index..(Count - 1)]);
        Count--;
    }

    public bool Remove(T item)
    {
        var index = IndexOf(item);
        if (index is -1) return false;
        RemoveAt(index);
        return true;
    }

    public T this[int index]
    {
        readonly get
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            return buffer[index];
        }
#pragma warning disable IDE0251
        set
        {
            if (index < 0 || index >= Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            buffer[index] = value;
        }
#pragma warning restore IDE0251
    }

    public readonly ValueList<T> this[Range range]
    {
        get
        {
            ValueList<T> result = [];
            var values = buffer[..Count][range];
            values.CopyTo(result.buffer);
            result.Count = values.Length;
            return result;
        }
    }

    public override readonly string ToString() => Mem.JoinString(buffer[..Count]);

    public static implicit operator ReadOnlySpan<T>(in ValueList<T> list) => list.buffer;

    public readonly IEnumerator<T> GetEnumerator()
    {
        for (var i = 0; i < Count; i++)
            yield return buffer[i];
    }

    readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public readonly bool Equals(ValueList<T> other) => buffer[..Count].SequenceEqual(other.buffer[..other.Count]);

    public override readonly bool Equals(object? obj) => obj is ValueList<T> other && Equals(other);

    // ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
    public override readonly int GetHashCode() => base.GetHashCode();

    public static bool operator ==(ValueList<T> left, ValueList<T> right) => left.Equals(right);

    public static bool operator !=(ValueList<T> left, ValueList<T> right) => !left.Equals(right);

    [InlineArray(MaxSize)]
    struct ValueListBuffer
    {
        T element0;
    }
}
