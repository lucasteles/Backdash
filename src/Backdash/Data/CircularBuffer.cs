namespace Backdash.Data;

using System.Collections;

public sealed class CircularBuffer<T>(int capacity) : IReadOnlyList<T> where T : notnull
{
    readonly T[] elements = new T[capacity];
    int head;

    public int Capacity => elements.Length;
    public int Length { get; private set; }

    int IReadOnlyCollection<T>.Count => Length;

    public bool IsEmpty => Length is 0;

    public ref T Peek() => ref elements[head];

    public void SetHeadTo(int index) => head = index;

    public void Clear()
    {
        Array.Clear(elements, 0, elements.Length);
        head = 0;
        Length = 0;
    }

    public ref T this[int idx] => ref elements[(head + idx) % elements.Length];

    T IReadOnlyList<T>.this[int idx] => elements[(head + idx) % elements.Length];

    public void Advance()
    {
        head = (head + 1) % Capacity;
        Length = Math.Min(Capacity, Length + 1);
    }

    public void Add(in T val)
    {
        elements[head] = val;
        Advance();
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (var i = 0; i < Length; i++)
            yield return this[i];
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
