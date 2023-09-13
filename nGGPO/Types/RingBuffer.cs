using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace nGGPO.Types;

sealed class RingBuffer<T> : IReadOnlyList<T> where T : notnull
{
    readonly T[] elements;

    public int Count => head >= tail ? head - tail : head + (Capacity - tail);

    public int Capacity => elements.Length;
    public bool IsEmpty => Count is 0;
    public bool IsFull => Count == Capacity;

    int head, tail;

    public RingBuffer(int size = 64) => elements = new T[size];

    public ref T Peek()
    {
        Trace.Assert(Count != elements.Length);
        return ref elements[tail];
    }

    public void Clear()
    {
        head = tail;
        Array.Clear(elements, 0, elements.Length);
    }

    public ref T Value(int idx) => ref elements[(tail + idx) % elements.Length];
    public T this[int idx] => Value(idx);

    public void Pop()
    {
        Trace.Assert(Count != Capacity);
        tail = (tail + 1) % Capacity;
    }

    public void Push(in T val)
    {
        Trace.Assert(Count != elements.Length - 1);
        elements[head] = val;
        head = (head + 1) % Capacity;
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (var i = 0; i < Count; i++)
            yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}