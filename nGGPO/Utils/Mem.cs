﻿using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace nGGPO.Utils;

public static class Mem
{
    public const int ByteSize = 8;

    const int MaxStackLimit = 1024;
    const int MaximumBufferSize = int.MaxValue;

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

    public static unsafe int MarshallStructure<T>(in T message, in Span<byte> body)
        where T : struct
    {
        var size = Marshal.SizeOf(message);

        nint ptr;

        if (size > MaxStackLimit)
            ptr = Marshal.AllocHGlobal(size);
        else
        {
            var stackPointer = stackalloc byte[size];
            ptr = (nint) stackPointer;
        }

        try
        {
            fixed (byte* bodyPtr = body)
            {
                Marshal.StructureToPtr(message, ptr, true);
                Span<byte> source = new((void*) ptr, size);
                Span<byte> dest = new(bodyPtr, size);
                source.CopyTo(dest);
            }
        }
        finally
        {
            if (size > MaxStackLimit)
                Marshal.FreeHGlobal(ptr);
        }

        return size;
    }

    public static unsafe T UnmarshallStructure<T>(in ReadOnlySpan<byte> body) where T : struct
    {
        var size = body.Length;

        nint ptr;

        if (size > MaxStackLimit)
            ptr = Marshal.AllocHGlobal(size);
        else
        {
            var stackPointer = stackalloc byte[size];
            ptr = (nint) stackPointer;
        }

        try
        {
            Span<byte> dest = new((void*) ptr, body.Length);
            body.CopyTo(dest);
            return Marshal.PtrToStructure<T>(ptr);
        }
        finally
        {
            if (size > MaxStackLimit)
                Marshal.FreeHGlobal(ptr);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<TElement> InlineArrayAsSpan<TBuffer, TElement>(
        scoped ref TBuffer buffer, int size) where TBuffer : struct =>
        MemoryMarshal.CreateSpan(ref Unsafe.As<TBuffer, TElement>(ref buffer), size);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<TElement> InlineArrayAsReadOnlySpan<TBuffer, TElement>(
        scoped in TBuffer buffer, int size) where TBuffer : struct =>
        MemoryMarshal.CreateReadOnlySpan(
            ref Unsafe.As<TBuffer, TElement>(ref Unsafe.AsRef(in buffer)), size);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<byte> StructAsSpan<TValue>(scoped ref TValue value)
        where TValue : struct =>
        MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref value, 1));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<byte> StructAsReadOnlySpan<TValue>(scoped in TValue value)
        where TValue : struct =>
        MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in value), 1));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TValue SpanAsStruct<TValue>(in ReadOnlySpan<byte> bytes)
        where TValue : struct =>
        MemoryMarshal.Cast<byte, TValue>(bytes)[0];

    public static T ReadUnaligned<T>(in ReadOnlySpan<byte> data) where T : struct
    {
        if (data.Length < Unsafe.SizeOf<T>())
            throw new ArgumentOutOfRangeException(nameof(data));

        return Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(data));
    }

    public static TInt EnumAsInteger<TEnum, TInt>(TEnum enumValue)
        where TEnum : unmanaged, Enum
        where TInt : unmanaged, IBinaryInteger<TInt>
    {
        if (Unsafe.SizeOf<TEnum>() != Unsafe.SizeOf<TInt>()) throw new Exception("type mismatch");
        return Unsafe.As<TEnum, TInt>(ref enumValue);
    }

    public static TEnum IntegerAsEnum<TEnum, TInt>(TInt intValue)
        where TEnum : unmanaged, Enum
        where TInt : unmanaged, IBinaryInteger<TInt>
    {
        if (Unsafe.SizeOf<TEnum>() != Unsafe.SizeOf<TInt>()) throw new Exception("type mismatch");
        return Unsafe.As<TInt, TEnum>(ref intValue);
    }

    // TODO: create non alloc version of this
    public static string GetBitString(
        in ReadOnlySpan<byte> bytes,
        int splitAt = 0,
        int bytePad = ByteSize)
    {
        var builder = new StringBuilder();

        for (var i = 0; i < bytes.Length; i++)
        {
            if (i > 0 && splitAt > 0 && i % splitAt is 0) builder.Append('-');

            var bin = Convert.ToString(bytes[i], 2).PadLeft(bytePad, '0');
            for (var j = bin.Length - 1; j >= 0; j--)
                builder.Append(bin[j]);
        }

        return builder.ToString();
    }
}