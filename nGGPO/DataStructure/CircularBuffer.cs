using System;
using nGGPO.Utils;

namespace nGGPO.DataStructure;

sealed class CircularBuffer<T>(int size = 64)
    where T : notnull
{
    readonly T[] elements = new T[size];
    int head, tail;

    public int Count => head >= tail ? head - tail : head + (Capacity - tail);

    public int Capacity => elements.Length;
    public bool IsEmpty => Count is 0;

    public ref T Peek() => ref elements[tail];

    public void Clear()
    {
        head = tail;
        Array.Clear(elements, 0, elements.Length);
    }

    public ref T this[int idx] => ref elements[(tail + idx) % elements.Length];

    public T Pop()
    {
        var value = elements[tail];
        tail = (tail + 1) % Capacity;
        return value;
    }

    public void Push(in T val)
    {
        elements[head] = val;
        head = (head + 1) % Capacity;
    }
}