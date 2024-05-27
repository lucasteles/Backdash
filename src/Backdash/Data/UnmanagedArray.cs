using System.Text;
using Backdash.Core;

namespace Backdash.Data;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

/// <summary>
/// An disposable unmanaged array
/// </summary>
/// <typeparam name="T"></typeparam>
[StructLayout(LayoutKind.Sequential)]
[DebuggerDisplay("Length = {Length}")]
[DebuggerTypeProxy(typeof(UnmanagedArrayDebugView<>))]
[CollectionBuilder(typeof(UnmanagedArrayCollectionBuilder), nameof(UnmanagedArrayCollectionBuilder.Create))]
public readonly unsafe struct UnmanagedArray<T>
    : IDisposable, IEquatable<UnmanagedArray<T>>, IReadOnlyList<T> where T : unmanaged
{
    readonly bool memoryPressure;

    /// <summary>
    /// Returns an empty <see cref="UnmanagedArray{T}"/>.
    /// </summary>
    public static readonly UnmanagedArray<T> Empty = new();

    readonly byte* buffer;

    /// <summary>
    /// Size of the array
    /// </summary>
    public int Length { get; }

    /// <summary>
    /// Initializes a new UnmanagedArray
    /// </summary>
    public UnmanagedArray()
    {
        Length = 0;
        buffer = null;
    }

    /// <summary>
    /// Initializes a new UnmanagedArray
    /// </summary>
    public UnmanagedArray(int size, bool zeroed = true, bool memoryPressure = false)
    {
        ThrowHelpers.ThrowIfArgumentIsNegative(size);
        Length = size;

        if (size is 0)
            buffer = null;
        else
            buffer = zeroed
                ? (byte*)NativeMemory.AllocZeroed(checked((nuint)size), (nuint)Unsafe.SizeOf<T>())
                : (byte*)NativeMemory.Alloc(checked((nuint)size), (nuint)Unsafe.SizeOf<T>());

        this.memoryPressure = memoryPressure;
        if (memoryPressure)
            GC.AddMemoryPressure((long)ByteSize);
    }

    /// <summary>
    /// Initializes a new UnmanagedArray with values from <paramref name="values"/>
    /// </summary>
    /// <param name="values"></param>
    public UnmanagedArray(ReadOnlySpan<T> values) : this(values.Length)
    {
        if (!values.IsEmpty)
            values.CopyTo(Span);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (!IsCreated || PointerAddress is 0)
            return;

        NativeMemory.Free(buffer);

        if (memoryPressure)
            GC.RemoveMemoryPressure((long)ByteSize);
    }

    nuint ByteSize => checked((nuint)Length) * (nuint)Unsafe.SizeOf<T>();

    /// <summary>
    /// Returns a value that indicates whether the current <see cref="UnmanagedArray{T}"/> is empty.
    /// </summary>
    public bool IsEmpty => Length is 0 || !IsCreated;

    /// <summary>
    /// Returns true if the array is initialized
    /// </summary>
    public bool IsCreated => buffer != null;

    /// <summary>
    /// Pointer address
    /// </summary>
    public nuint PointerAddress => (nuint)buffer;

    /// <summary>
    /// Returns a span for the current array
    /// </summary>
    public Span<T> Span => new(buffer, Length);

    /// <summary>
    /// Returns a readonly span for the current array
    /// </summary>
    public ReadOnlySpan<T> ReadOnlySpan => new(buffer, Length);

    /// <summary>
    /// Returns a memory for the current array
    /// </summary>
    public Memory<T> ToMemory() => ToMemory(0);

    /// <summary>
    /// Returns a memory for the current array starting at <paramref name="start"/>
    /// </summary>
    public Memory<T> ToMemory(long start)
    {
        if ((ulong)start > (ulong)Length) ThrowHelpers.ThrowArgumentOutOfRangeException(nameof(start));
        return ToMemory(start, checked((int)(Length - start)));
    }

    /// <summary>
    /// Returns a memory for the current array starting at <paramref name="start"/> with size <paramref name="length"/>
    /// </summary>
    public Memory<T> ToMemory(long start, int length)
    {
        if ((ulong)(start + length) > (ulong)(Length))
            ThrowHelpers.ThrowArgumentOutOfRangeException(nameof(length));
        return new NativeMemoryManager<T>(buffer + (start * Unsafe.SizeOf<T>()), length).Memory;
    }

    /// <inheritdoc cref="IEnumerable.GetEnumerator()"/>
    public Enumerator GetEnumerator() => new(this);

    /// <inheritdoc />
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();

    /// <inheritdoc />
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Indicates whether the objects are equal to each another.
    /// </summary>
    public static bool Equals(in UnmanagedArray<T> left, in UnmanagedArray<T> right)
    {
        if (left.IsCreated != right.IsCreated)
            return false;

        if (left.buffer == right.buffer && left.Length == right.Length)
            return true;

        return left.Span.SequenceEqual(right);
    }

    /// <inheritdoc />
    public bool Equals(UnmanagedArray<T> other) => Equals(in this, in other);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => obj is UnmanagedArray<T> other && Equals(in this, in other);

    int IReadOnlyCollection<T>.Count => Length;
    T IReadOnlyList<T>.this[int index] => this[index];

    /// <inheritdoc cref="Span" />
    public static implicit operator Span<T>(in UnmanagedArray<T> array) => array.Span;

    /// <inheritdoc cref="ReadOnlySpan" />
    public static implicit operator ReadOnlySpan<T>(in UnmanagedArray<T> array) => array.ReadOnlySpan;

    /// <inheritdoc cref="Equals(Backdash.Data.UnmanagedArray{T})" />
    public static bool operator ==(in UnmanagedArray<T> left, in UnmanagedArray<T> right) => Equals(in left, in right);

    /// <inheritdoc cref="Equals(Backdash.Data.UnmanagedArray{T})" />
    public static bool operator !=(in UnmanagedArray<T> left, in UnmanagedArray<T> right) => !Equals(in left, in right);

    /// <summary>
    /// Forms a new Array for the slice out of the current span starting at a specified index for a specified length.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public Span<T> Slice(int start, int length)
    {
        if (start > Length || length > Length - start)
#pragma warning disable S3928
            throw new ArgumentOutOfRangeException();
#pragma warning restore S3928

        var startAddress = PointerAddress + (nuint)start;
        return new(startAddress.ToPointer(), length);
    }

    /// <summary>
    /// Returns a reference to specified element of the <see cref="EquatableArray{T}"/>.
    /// </summary>
    /// <param name="index">array index</param>
    public ref T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((ulong)index >= (ulong)Length) ThrowHelpers.ThrowIndexOutOfRangeException();
            var memoryIndex = index * Unsafe.SizeOf<T>();
            return ref Unsafe.AsRef<T>(buffer + memoryIndex);
        }
    }

    /// <summary>
    /// Returns a reference to specified element at <paramref name="index"/> of the <see cref="EquatableArray{T}"/>.
    /// </summary>
    /// <param name="index">array index</param>
    public ref T this[Index index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => ref Span[index];
    }

    /// <summary>
    /// Returns a rage slice as a copy of the current <see cref="EquatableArray{T}"/>
    /// </summary>
    public Span<T> this[Range range]
    {
        get
        {
            var (offset, length) = range.GetOffsetAndLength(Length);
            return Slice(offset, length);
        }
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var value = MemoryMarshal.AsBytes(Span);
        HashCode hash = new();
        hash.AddBytes(value);
        return hash.ToHashCode();
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        const string separator = ", ";
        const char prefix = '[';
        const char suffix = ']';
        StringBuilder builder = new(Length * 2);
        builder.Append(prefix);
        for (var i = 0; i < Length; i++)
        {
            if (i > 0) builder.Append(separator);
            if (this[i] is var value)
                builder.Append(value);
        }

        builder.Append(suffix);
        return builder.ToString();
    }

    /// <summary>
    /// Returns a new array of the current <see cref="UnmanagedArray{T}"/>.
    /// </summary>
    public T[] ToArray() => Span.ToArray();

    /// <summary>
    /// Creates new stream over this unmanaged memory
    /// </summary>
    public Stream ToStream(long offset = 0, FileAccess fileAccess = FileAccess.Read)
    {
        if ((ulong)offset > (ulong)Length)
            ThrowHelpers.ThrowArgumentOutOfRangeException(nameof(offset));
        var size = (int)ByteSize;
        return new UnmanagedMemoryStream(buffer + (offset * Unsafe.SizeOf<T>()), size, size, fileAccess);
    }

    /// <inheritdoc cref="MemoryExtensions.CopyTo{T}(T[], Span{T})"/>
    public void CopyTo(Span<T> destination) => Span.CopyTo(destination);

    /// <inheritdoc cref="MemoryExtensions.CopyTo{T}(T[], Memory{T})"/>
    public void CopyTo(Memory<T> destination) => Span.CopyTo(destination.Span);


    /// <summary>
    /// Sets all elements in an array to the default value of each element type.
    /// </summary>
    public void Clear(bool nativeClear = false)
    {
        if (nativeClear)
            NativeMemory.Clear(buffer, ByteSize);
        else
            Span.Clear();
    }

    /// <summary>
    /// Sets a range of elements in the array to the default value of each element type.
    /// </summary>
    /// <param name="index"></param>
    /// <param name="length"></param>
    public void Clear(int index, int length) => Span.Slice(index, length).Clear();

    /// <summary>
    /// Sets a range of elements in an array to the default value of each element type.
    /// </summary>
    /// <param name="range">Slice to be clean</param>
    public void Clear(Range range)
    {
        var (offset, length) = range.GetOffsetAndLength(Length);
        Clear(offset, length);
    }

    /// <summary>
    ///  Assigns the given value of type T to each element of the specified array.
    /// </summary>
    public void Fill(T value) => Span.Fill(value);

    /// <summary>
    ///  Assigns the given return value from delegate of type T to each element of the specified array.
    /// </summary>
    public void Fill(Func<T> value)
    {
        var values = Span;
        for (var i = 0; i < values.Length; i++)
            values[i] = value();
    }

    /// <inheritdoc cref="MemoryExtensions.Reverse{T}"/>
    public void Reverse() => Span.Reverse();


    /// <summary>
    ///  Create and allocates a new UnmanagedArray with same values
    /// </summary>
    public UnmanagedArray<T> Clone()
    {
        if (!IsCreated) return new();
        UnmanagedArray<T> result = new(Length);
        CopyTo(result);
        return result;
    }

    /// <inheritdoc />
    public struct Enumerator : IEnumerator<T>
    {
        readonly UnmanagedArray<T> array;
        int index;
        T current;

        internal Enumerator(UnmanagedArray<T> array)
        {
            this.array = array;
            index = 0;
            current = default;
        }

        /// <inheritdoc />
        public readonly void Dispose() { }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public readonly T Current => current;

        readonly object IEnumerator.Current
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

/// <summary>
/// DebuggerTypeProxy for <see cref="UnmanagedArray{T}"/>
/// </summary>
sealed unsafe class UnmanagedArrayDebugView<T>(UnmanagedArray<T> array)
    where T : unmanaged
{
    public T[]? Items
    {
        get
        {
            if (!array.IsCreated)
                return default;

            var length = array.Length;
            var dst = new T[length];

            var handle = GCHandle.Alloc(dst, GCHandleType.Pinned);
            var addr = handle.AddrOfPinnedObject();

            Unsafe.CopyBlock((void*)addr, (void*)array.PointerAddress, (uint)(length * Unsafe.SizeOf<T>()));

            handle.Free();
            return dst;
        }
    }
}

/// <summary>
/// Initialization methods for instances of <see cref="EquatableArray{T}"/>.
/// </summary>
public static class UnmanagedArrayCollectionBuilder
{
    /// <summary>
    /// Produce an <see cref="UnmanagedArray{T}"/> of contents from specified elements.
    /// </summary>
    /// <typeparam name="T">The type of element in the array.</typeparam>
    /// <param name="items">The elements to store in the array.</param>
    /// <returns>An array containing the specified items.</returns>
    public static UnmanagedArray<T> Create<T>(ReadOnlySpan<T> items) where T : unmanaged => new(items);
}
