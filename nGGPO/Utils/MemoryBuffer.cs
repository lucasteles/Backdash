using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace nGGPO.Utils;

public readonly struct MemoryBuffer<T> : IDisposable
{
    readonly bool clearArray;

    readonly T[] array;
    public int Length { get; }
    public Memory<T> Memory { get; }

    internal MemoryBuffer(int size, bool clearArray = false)
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

public static class MemoryBuffer
{
    const int MaximumBufferSize = int.MaxValue;

    public static MemoryBuffer<T> Empty<T>() => new(0);

    public static MemoryBuffer<T> Rent<T>(int size = -1, bool clearArray = false)
    {
        if (size == -1)
            size = 1 + 4095 / Unsafe.SizeOf<T>();
        else if ((uint) size > MaximumBufferSize)
            throw new ArgumentOutOfRangeException(nameof(size));

        return new(size, clearArray);
    }

    public static MemoryBuffer<byte> Rent(int size, bool clearArray = false) =>
        Rent<byte>(size, clearArray);
}