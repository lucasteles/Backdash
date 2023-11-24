using System;
using System.Buffers;

namespace nGGPO.Utils;

public readonly struct MemoryBuffer<T> : IDisposable
{
    readonly bool clearArray;
    public static readonly MemoryBuffer<T> Empty = new(0);

    readonly T[] array;
    public int Length { get; }
    public Memory<T> Memory { get; }

    public MemoryBuffer(int size, bool clearArray = false)
    {
        var buffer =
            size is 0
                ? Array.Empty<T>()
                : ArrayPool<T>.Shared.Rent(size);

        array = buffer;
        Memory = buffer;
        Length = size;
        this.clearArray = clearArray;
    }

    public Span<T> Span => Memory.Span;
    public ref T this[int index] => ref Span[index];

    public void Dispose()
    {
        if (array?.Length > 0)
            ArrayPool<T>.Shared.Return(array, clearArray);
    }

    public static implicit operator Span<T>(MemoryBuffer<T> @this) => @this.Span;
    public static implicit operator ReadOnlySpan<T>(MemoryBuffer<T> @this) => @this.Span;
}