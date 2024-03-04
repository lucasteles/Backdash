using System.Collections;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

// ReSharper disable ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
namespace Backdash.Data;

[CollectionBuilder(typeof(ArrayCollectionBuilder), nameof(ArrayCollectionBuilder.Create))]
public sealed class Array<T>(T[] values) :
    IReadOnlyList<T>,
    IEquatable<Array<T>>,
    IEqualityOperators<Array<T>, Array<T>, bool>
    where T : IEquatable<T>
{
    public Array(int size) : this(new T[size]) { }
    public Array() : this([]) { }

    public static readonly Array<T> Empty = [];

    readonly T[] values = values;

    public int Length => values.Length;
    public bool IsEmpty => values.Length is 0;
    public IReadOnlyList<T> AsReadOnly() => values.AsReadOnly();
    public T[] AsArray() => values;
    public Span<T> AsSpan() => values;
    public Memory<T> AsMemory() => values;

    public void CopyTo(Span<T> destination) => values.CopyTo(destination);
    public void CopyTo(Memory<T> destination) => values.CopyTo(destination);
    public void CopyTo(Array<T> destination) => values.CopyTo(destination.AsSpan());

    public ref T this[int index] => ref values[index];
    public ref T this[Index index] => ref values[index];
    public Array<T> this[Range range] => new(values[range]);

    public void Clear(int index, int length) => Array.Clear(values, index, length);

    public void Clear(Range range)
    {
        var (offset, length) = range.GetOffsetAndLength(values.Length);
        Array.Clear(values, offset, length);
    }

    public void Clear() => Clear(0, values.Length);

    public bool Equals(Array<T>? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other) || ReferenceEquals(values, other.values)) return true;
        return values.AsSpan().SequenceEqual(other.values);
    }

    public override bool Equals(object? obj) =>
        ReferenceEquals(this, obj) || (obj is Array<T> other && Equals(other));

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = 17;
            for (var index = 0; index < values.Length; index++)
            {
                ref readonly var item = ref values[index];
                hash = (hash * 31) + (item is null ? 0 : EqualityComparer<T>.Default.GetHashCode(item));
            }

            return hash;
        }
    }

    int IReadOnlyCollection<T>.Count => Length;
    T IReadOnlyList<T>.this[int index] => values[index];

    /// <summary>
    ///  Determines whether the specified array contains elements that match the conditions defined by the specified predicate.
    /// </summary>
    public bool Exist(Predicate<T> predicate) => Array.Exists(values, predicate);

    /// <summary>
    ///  Filters an array of values based on a predicate.
    /// </summary>
    public Array<T> FindAll(Predicate<T> predicate) => new(Array.FindAll(values, predicate));

    /// <summary>
    ///  Searches for an element that matches the conditions defined by the specified predicate, and returns the first occurrence within the entire Array.
    /// </summary>
    public T? Find(Predicate<T> predicate) => Array.Find(values, predicate);

    /// <summary>
    ///  Searches for an element that matches the conditions defined by the specified predicate, and returns the last occurrence within the entire Array.
    /// </summary>
    public T? FindLast(Predicate<T> predicate) => Array.FindLast(values, predicate);

    /// <summary>
    ///  Searches for an element that matches the conditions defined by the specified predicate, and returns the zero-based index of the first occurrence within the entire Array.
    /// </summary>
    public int FindIndex(Predicate<T> predicate) => Array.FindIndex(values, predicate);

    /// <summary>
    ///  Searches for an element that matches the conditions defined by the specified predicate, and returns the zero-based index of the last occurrence within the entire Array.
    /// </summary>
    public int FindLastIndex(Predicate<T> predicate) => Array.FindLastIndex(values, predicate);

    /// <summary>
    ///  Changes the number of elements of a one-dimensional array to the specified new size.
    /// </summary>
    public void Resize(int newSize) => Array.Resize(ref Unsafe.AsRef(in values), newSize);

    /// <summary>
    ///  Assigns the given value of type T to each element of the specified array.
    /// </summary>
    public void Fill(T value) => Array.Fill(values, value);

    /// <summary>
    ///  Assigns the given return value from delegate of type T to each element of the specified array.
    /// </summary>
    public void Fill(Func<T> value)
    {
        for (var i = 0; i < values.Length; i++)
            values[i] = value();
    }

    /// <summary>
    ///  Crates new array with new size copying elements from source
    /// </summary>
    public Array<T> ToResized(int newSize)
    {
        var copy = new T[newSize];
        var size = Math.Min(newSize, values.Length);
        Array.Copy(values, copy, size);
        return new(copy);
    }

    /// <summary>
    ///  Create new array with same values
    /// </summary>
    public Array<T> Clone()
    {
        var copy = new T[Length];
        Array.Copy(values, copy, values.Length);
        return new(copy);
    }

    /// <summary>
    ///  Reverses the sequence of the elements in the one-dimensional generic array.
    /// </summary>
    public void Reverse() => Array.Reverse(values);

    /// <summary>
    ///  Reverses the sequence of a subset of the elements in the one-dimensional generic array.
    /// </summary>
    public void Reverse(int index, int length) => Array.Reverse(values, index, length);

    /// <summary>
    ///  Reverses the sequence of a subset of the elements in the one-dimensional generic array.
    /// </summary>
    public void Reverse(Range range)
    {
        var (offset, length) = range.GetOffsetAndLength(values.Length);
        Reverse(offset, length);
    }

    /// <summary>
    ///  Sorts the elements of an array in ascending order.
    /// </summary>
    public void Sort(IComparer<T>? comparer = null) => Array.Sort(values, comparer);

    /// <summary>
    ///  Sorts the elements in a range of elements in an Array
    /// </summary>
    public void Sort(int index, int length, IComparer<T>? comparer = null) =>
        Array.Sort(values, index, length, comparer);

    /// <summary>
    ///  Sorts the elements in a range of elements in an Array
    /// </summary>
    public void Sort(Range range, IComparer<T>? comparer = null)
    {
        var (offset, length) = range.GetOffsetAndLength(values.Length);
        Sort(offset, length, comparer);
    }

    /// <summary>
    ///  Sorts the elements of an array in ascending order into a new array.
    /// </summary>
    public Array<T> ToSorted(IComparer<T>? comparer = null)
    {
        var copy = Clone();
        copy.Sort(comparer);
        return copy;
    }

    /// <summary>
    ///  Sorts the elements in a range of elements in an Array into a new array
    /// </summary>
    public Array<T> ToSorted(int index, int length, IComparer<T>? comparer = null)
    {
        var copy = Clone();
        copy.Sort(index, length, comparer);
        return copy;
    }

    /// <summary>
    ///  Sorts the elements in a range of elements in an Array into a new array
    /// </summary>
    public Array<T> ToSorted(Range range, IComparer<T>? comparer = null)
    {
        var copy = Clone();
        copy.Sort(range, comparer);
        return copy;
    }

    /// <summary>
    ///  Sorts the elements of an array in ascending order according to a key.
    /// </summary>
    public void SortBy<TKey>(Func<T, TKey> selector, IComparer<TKey>? comparer = null) where TKey : IComparable<TKey>
    {
        if (comparer is not null)
            Array.Sort(values, (x, y) => comparer.Compare(selector(x), selector(y)));
        else
            Array.Sort(values, (x, y) => Comparer<TKey>.Default.Compare(selector(x), selector(y)));
    }

    /// <summary>
    ///  Sorts the elements of an array in ascending order according to a key.
    /// </summary>
    public Array<T> ToSortedBy<TKey>(Func<T, TKey> selector, IComparer<TKey>? comparer = null)
        where TKey : IComparable<TKey>
    {
        var copy = Clone();
        copy.SortBy(selector, comparer);
        return copy;
    }

    /// <summary>
    ///  Sorts the elements of an array in ascending order according to a key.
    /// </summary>
    public Array<TOutput> Map<TOutput>(Func<T, TOutput> projection)
        where TOutput : IEquatable<TOutput>
    {
        TOutput[] newArray = new TOutput[values.Length];
        for (int i = 0; i < values.Length; i++)
            newArray[i] = projection(values[i]);

        return new(newArray);
    }

    public override string ToString()
    {
        const string separator = ", ";
        const string nullValue = "null";
        const char prefix = '[';
        const char suffix = ']';
        StringBuilder builder = new(Length * 2);
        builder.Append(prefix);
        for (var i = 0; i < values.Length; i++)
        {
            if (i > 0) builder.Append(separator);

            if (values[i] is { } value)
                builder.Append(value);
            else
                builder.Append(nullValue);
        }

        builder.Append(suffix);
        return builder.ToString();
    }

    public Enumerator GetEnumerator() => new(this);
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public static implicit operator T[](Array<T> array) => array.values;
    public static implicit operator Span<T>(Array<T> array) => array.values;
    public static implicit operator ReadOnlySpan<T>(Array<T> array) => array.values;
    public static implicit operator Memory<T>(Array<T> array) => array.values;
    public static bool operator ==(Array<T>? left, Array<T>? right) => Equals(left, right);
    public static bool operator !=(Array<T>? left, Array<T>? right) => !Equals(left, right);

    public struct Enumerator : IEnumerator<T>
    {
        readonly Array<T> array;
        int index;
        T? current;

        internal Enumerator(Array<T> array)
        {
            this.array = array;
            index = 0;
            current = default;
        }

        public readonly void Dispose() { }

        public bool MoveNext()
        {
            if ((uint)index < (uint)array.Length)
            {
                current = array[index];
                index++;
                return true;
            }

            index = array.Length + 1;
            current = default;
            return false;
        }

        public readonly T Current => current!;

        readonly object? IEnumerator.Current
        {
            get
            {
                if (index == array.Length + 1)
                    throw new InvalidOperationException("Index out of range");

                return Current;
            }
        }

        void IEnumerator.Reset()
        {
            index = 0;
            current = default;
        }
    }
}

public static class ArrayCollectionBuilder
{
    public static Array<T> Create<T>(ReadOnlySpan<T> items) where T : IEquatable<T> => new(items.ToArray());
}
