using System.Threading.Channels;

namespace nGGPO.Data;

sealed class CircularBuffer<T>(int capacity = 64)
    where T : notnull
{
    readonly T[] elements = new T[capacity];
    int head, tail;

    public int Capacity => elements.Length;
    public int Size { get; private set; }

    public bool IsEmpty => Size is 0;

    public ref T Peek() => ref elements[tail];

    public void Clear()
    {
        head = tail;
        Array.Clear(elements, 0, elements.Length);
        Size = 0;
    }

    public ref T this[int idx] => ref elements[(tail + idx) % elements.Length];

    public ref T Pop()
    {
        ref var value = ref Peek();

        tail = (tail + 1) % Capacity;
        Size--;

        return ref value!;
    }

    public void Push(in T val)
    {
        elements[head] = val;
        head = (head + 1) % Capacity;
        Size++;
    }
}

public static class CircularBuffer
{
    const int DefaultSize = 64;

    public static Channel<T> CreateChannel<T>(int size = DefaultSize) =>
        Channel.CreateBounded<T>(
            new BoundedChannelOptions(size)
            {
                SingleWriter = true,
                SingleReader = true,
                AllowSynchronousContinuations = true,
                FullMode = BoundedChannelFullMode.DropOldest,
            });
}
