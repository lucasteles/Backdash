using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace nGGPO.Utils;

public static class Mem
{
    public const int ByteSize = 8;

    const int MaxStackLimit = 1024;
    const int MaximumBufferSize = int.MaxValue;

    public static MemoryBuffer<T> Rent<T>(int size = -1)
    {
        if (size == -1)
            size = 1 + 4095 / Unsafe.SizeOf<T>();
        else if ((uint) size > MaximumBufferSize)
            throw new ArgumentOutOfRangeException(nameof(size));

        return new(size);
    }

    public static MemoryBuffer<byte> Rent(int size) => Rent<byte>(size);

    public static bool BytesEqual(ReadOnlySpan<byte> a1, ReadOnlySpan<byte> a2) =>
        a1.Length == a2.Length && a1.SequenceEqual(a2);

    public static MemoryBuffer<byte> StructToBytes<T>(T message)
        where T : struct
    {
        var size = Marshal.SizeOf(message);
        var buffer = Rent(size);
        StructToBytes(message, buffer);
        return buffer;
    }

    public unsafe static void StructToBytes<T>(T message, Span<byte> body)
        where T : struct
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
    }

    public static unsafe T BytesToStruct<T>(ReadOnlySpan<byte> body) where T : struct
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

    public static TInt EnumAsInteger<TEnum, TInt>(TEnum enumValue)
        where TEnum : unmanaged, Enum
        where TInt : unmanaged
    {
        if (Unsafe.SizeOf<TEnum>() != Unsafe.SizeOf<TInt>()) throw new Exception("type mismatch");
        return Unsafe.As<TEnum, TInt>(ref enumValue);
    }

    public static TEnum IntegerAsEnum<TEnum, TInt>(TInt intValue)
        where TEnum : unmanaged, Enum
        where TInt : unmanaged
    {
        if (Unsafe.SizeOf<TEnum>() != Unsafe.SizeOf<TInt>()) throw new Exception("type mismatch");
        return Unsafe.As<TInt, TEnum>(ref intValue);
    }

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