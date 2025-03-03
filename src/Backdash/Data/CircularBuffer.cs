using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Backdash.Data;

/// <summary>
/// A collection data structure that uses a single fixed-size buffer as if it were connected end-to-end.
/// </summary>
[DebuggerDisplay("Size = {size}")]
public sealed class CircularBuffer<T>(int capacity) : IReadOnlyList<T>, IEquatable<CircularBuffer<T>>
{
    readonly T[] array = new T[capacity];
    int head, tail;
    int size;

    public int Size => size;
    public int HeadIndex => head;
    public int TailIndex => tail;

    public int CurrentIndex
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => head is 0 ? array.Length - 1 : head - 1;
    }

    public void Add(in T item)
    {
        array[head] = item;

        if (IsFull)
            DropFirst();
        else
        {
            size++;
            Trace.Assert(size <= array.Length);
        }

        head = (head + 1) % array.Length;
    }

    public T Drop()
    {
        if (size is 0)
            throw new InvalidOperationException("Can't pop from an empty buffer");

        var value = DropFirst();
        size--;
        return value;
    }

    public ref T Current() => ref array[CurrentIndex];
    public ref T Last() => ref array[tail];

    public T Peek()
    {
        if (size is 0)
            throw new InvalidOperationException("Can't peek from an empty buffer");

        return array[CurrentIndex];
    }

    public void AddRange(in ReadOnlySpan<T> values)
    {
        ref var curr = ref MemoryMarshal.GetReference(values);
        ref var end = ref Unsafe.Add(ref curr, values.Length);

        while (Unsafe.IsAddressLessThan(ref curr, ref end))
        {
            Add(curr);
            curr = ref Unsafe.Add(ref curr, 1)!;
        }
    }

    public void AddRange(ReadOnlySpan<T> values) => AddRange(in values);

    public bool TryPop([NotNullWhen(true)] out T? item)
    {
        if (size is 0)
        {
            item = default;
            return false;
        }

        item = Drop()!;
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    T DropFirst()
    {
        var firstIndex = head is 0 ? 0 : head - 1;
        var value = array[firstIndex];
        tail = (tail + 1) % array.Length;
        return value;
    }

    public int Capacity => array.Length;
    public bool IsEmpty => size is 0;
    public bool IsFull => size >= array.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T At(int index) => ref array[(tail + index) % array.Length];

    public ref T this[int index] => ref At(index);

    public ref T this[Index index] => ref At(index.GetOffset(Size));

    public void Clear(bool clearArray = false)
    {
        head = tail;
        size = 0;

        if (clearArray)
            Array.Clear(array, 0, array.Length);
    }

    T IReadOnlyList<T>.this[int index] => At(index);
    int IReadOnlyCollection<T>.Count => size;

    public override string ToString() => $"[{string.Join(", ", array)}]";

    public bool Equals(CircularBuffer<T>? other, EqualityComparer<T> comparer)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;

        for (var i = 0; i < size; i++)
            if (!comparer.Equals(At(i), other.At(i)))
                return false;

        return true;
    }

    public bool Equals(CircularBuffer<T>? other) => Equals(other, EqualityComparer<T>.Default);

    /// <inheritdoc/>
    public override bool Equals(object? obj) =>
        ReferenceEquals(this, obj) || (obj is CircularBuffer<T> other && Equals(other));

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var span = array.AsSpan(0, Size);
        ref var curr = ref MemoryMarshal.GetReference(span);
        ref var end = ref Unsafe.Add(ref curr, span.Length);
        HashCode hash = new();
        while (Unsafe.IsAddressLessThan(ref curr, ref end))
        {
            hash.Add(curr);
            curr = ref Unsafe.Add(ref curr, 1)!;
        }

        return hash.ToHashCode();
    }

    public int GetSpan(out ReadOnlySpan<T> begin, out ReadOnlySpan<T> end)
    {
        var items = array.AsSpan();
        var headItem = head is 0 && size > 0 ? items.Length : head;

        if (tail < headItem)
        {
            begin = items[tail..headItem];
            end = [];
        }
        else
        {
            begin = items[tail..];
            end = items[..head];
        }

        return size;
    }

    public void CopyFrom(ReadOnlySpan<T> values) => values.CopyTo(GetSpanAndReset(values.Length));

    public Span<T> GetSpanAndReset(int spanSize, bool clearArray = false)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(spanSize, array.Length);

        tail = 0;
        size = head = spanSize;

        if (clearArray)
            Array.Clear(array, 0, array.Length);

        return array.AsSpan(0, spanSize);
    }

    public void Fill(T value) => Array.Fill(array, value);

    public void FillWith(Func<T> valueFn)
    {
        for (var i = 0; i < array.Length; i++)
            array[i] = valueFn();
    }

    public void Discard(int offset)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        tail = (tail + offset) % array.Length;
        size -= offset;
        if (size < 0) size = 0;
    }

    public Enumerator GetEnumerator() => new(this);

    /// <inheritdoc />
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public T[] ToArray()
    {
        var result = new T[size];

        for (int i = 0; i < size; i++)
            result[i] = At(i);

        return result;
    }

    public struct Enumerator : IEnumerator<T>
    {
        readonly CircularBuffer<T> buffer;
        int index;
        T? current;

        internal Enumerator(CircularBuffer<T> buffer) =>
            this.buffer = buffer;

        /// <inheritdoc />
        public readonly void Dispose() { }

        /// <inheritdoc />
        public bool MoveNext()
        {
            if (index < buffer.size)
            {
                current = buffer[index];
                index++;
                return true;
            }

            current = default;
            return false;
        }

        /// <inheritdoc />
        public readonly T Current => current!;

        readonly object? IEnumerator.Current
        {
            get
            {
                if (index > buffer.size)
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
