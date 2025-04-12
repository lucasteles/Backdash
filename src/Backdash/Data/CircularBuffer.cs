using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Backdash.Core;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

namespace Backdash.Data;

/// <summary>
///     A collection data structure that uses a single fixed-size buffer as if it were connected end-to-end.
/// </summary>
[DebuggerDisplay("Size = {count}")]
public sealed class CircularBuffer<T>(int capacity) : IReadOnlyList<T>, IEquatable<CircularBuffer<T>>
{
    readonly T[] array = new T[capacity];
    int head, tail;
    int count;

    public int Size => count;
    public int LastIndex => tail;

    public int CurrentIndex
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => head is 0 ? array.Length - 1 : head - 1;
    }

    public void Add(in T item)
    {
        array[head] = item;

        if (IsFull)
            DropLast();
        else
        {
            count++;
            ThrowIf.Assert(count <= array.Length);
        }

        head = (head + 1) % array.Length;
    }

    public T Drop()
    {
        if (count is 0)
            throw new InvalidOperationException("Can't pop from an empty buffer");

        var value = DropLast();
        count--;
        return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    T DropLast()
    {
        var value = Back();
        tail = (tail + 1) % array.Length;
        return value;
    }


    public ref T Next() => ref array[head];
    public ref T Front() => ref array[CurrentIndex];
    public ref T Back() => ref array[tail];

    public void AddRange(ReadOnlySpan<T> values)
    {
        ref var curr = ref MemoryMarshal.GetReference(values);
        ref var end = ref Unsafe.Add(ref curr, values.Length);

        while (Unsafe.IsAddressLessThan(ref curr, ref end))
        {
            Add(curr);
            curr = ref Unsafe.Add(ref curr, 1)!;
        }
    }

    public bool TryDrop([NotNullWhen(true)] out T? item)
    {
        if (count is 0)
        {
            item = default;
            return false;
        }

        item = Drop()!;
        return true;
    }

    public int Capacity => array.Length;
    public bool IsEmpty => count is 0;
    public bool IsFull => count >= array.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T At(int index) => ref array[(tail + index) % array.Length];

    internal ref T AtRaw(int index) => ref array[index % array.Length];

    public ref T this[int index] => ref At(index);

    public ref T this[Index index] => ref At(index.GetOffset(Size));

    public void Clear(bool clearArray = false)
    {
        head = tail;
        count = 0;

        if (clearArray)
            Array.Clear(array, 0, array.Length);
    }

    T IReadOnlyList<T>.this[int index] => At(index);
    int IReadOnlyCollection<T>.Count => count;

    public override string ToString() => $"[{string.Join(", ", array)}]";

    static bool Equals(in CircularBuffer<T>? a, in CircularBuffer<T>? b, EqualityComparer<T>? comparer = null)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;

        comparer ??= EqualityComparer<T>.Default;
        for (var i = 0; i < a.count; i++)
            if (!comparer.Equals(a.At(i), b.At(i)))
                return false;

        return true;
    }

    public bool Equals(CircularBuffer<T>? other, EqualityComparer<T> comparer) => Equals(this, in other, comparer);

    /// <inheritdoc />
    public bool Equals(CircularBuffer<T>? other) => Equals(this, in other);

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is CircularBuffer<T> other && Equals(other);

    public static bool operator ==(in CircularBuffer<T> a, in CircularBuffer<T> b) => Equals(in a, in b);
    public static bool operator !=(CircularBuffer<T> a, CircularBuffer<T> b) => !Equals(in a, in b);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        HashCode hash = new();
        for (var i = 0; i < Size; i++) hash.Add(At(i));
        return hash.ToHashCode();
    }

    public void Fill(T value) => Array.Fill(array, value);

    public void FillWith(Func<T> valueFn)
    {
        for (var i = 0; i < array.Length; i++)
            array[i] = valueFn();
    }

    public void Advance(int offset = 1)
    {
        head = (head + offset) % array.Length;

        if (head < 0)
            head += array.Length;

        count += offset;
        CheckCount();
    }

    public void Discard(int offset = 1)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        tail = (tail + offset) % array.Length;
        count -= offset;
        CheckCount();
    }

    void CheckCount()
    {
        if (count < 0)
        {
            count = 0;
            head = tail;
        }
        else if (count > array.Length)
        {
            count = array.Length;
            tail = head;
        }
    }

    public void CopyTo(Span<T> destination)
    {
        if (destination.Length < GetSpan(out var begin, out var end))
            throw new ArgumentException("Destination is too short", nameof(destination));

        begin.CopyTo(destination);
        end.CopyTo(destination[begin.Length..]);
    }

    public void CopyFrom(ReadOnlySpan<T> values) => values.CopyTo(GetResetSpan(values.Length));

    public int GetSpan(out ReadOnlySpan<T> begin, out ReadOnlySpan<T> end)
    {
        var items = array.AsSpan();
        var headItem = head is 0 && count > 0 ? items.Length : head;

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

        return count;
    }


    public Span<T> GetResetSpan(int size, bool clearArray = false)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(size, array.Length);
        if (array.Length is 0) return [];

        tail = 0;
        count = size;
        head = size % array.Length;

        var result = array.AsSpan(0, size);
        if (clearArray) result.Clear();
        return result;
    }

    public Enumerator GetEnumerator() => new(this);

    /// <inheritdoc />
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public T[] ToArray()
    {
        var result = new T[count];

        for (int i = 0; i < count; i++)
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
            if (index < buffer.count)
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
                if (index > buffer.count)
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
