using System;
using nGGPO.Utils;

namespace nGGPO.DataStructure;

sealed class CircularBuffer<T>(int size = CircularBuffer<T>.DefaultSize)
    where T : notnull
{
    public const int DefaultSize = 64;

    readonly T[] elements = new T[size];

    public int Count => head >= tail ? head - tail : head + (Capacity - tail);

    public int Capacity => elements.Length;
    public bool IsEmpty => Count is 0;

    int head, tail;

    public ref T Peek()
    {
        Tracer.Assert(Count != elements.Length);
        return ref elements[tail];
    }

    public void Clear()
    {
        head = tail;
        Array.Clear(elements, 0, elements.Length);
    }

    public ref T this[int idx] => ref elements[(tail + idx) % elements.Length];

    public T Pop()
    {
        Tracer.Assert(Count != Capacity);
        var value = elements[tail];
        tail = (tail + 1) % Capacity;
        return value;
    }

    public void Push(in T val)
    {
        Tracer.Assert(Count != elements.Length - 1);
        elements[head] = val;
        head = (head + 1) % Capacity;
    }
}