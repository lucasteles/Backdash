using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace nGGPO.Utils;

public readonly struct PooledBuffer : IDisposable
{
    public readonly byte[] Bytes;

    public PooledBuffer(int size) => Bytes = Mem.Rent<byte>(size);

    public int Length => Bytes.Length;

    public static readonly PooledBuffer Empty = new();

    public void Dispose()
    {
        if (Bytes?.Length > 0)
            Mem.Return(Bytes, true);
    }

    public static implicit operator byte[](PooledBuffer @this) => @this.Bytes;
    public static implicit operator ReadOnlySpan<byte>(PooledBuffer @this) => @this.Bytes;
    public static implicit operator Span<byte>(PooledBuffer @this) => @this.Bytes;
}

public static class Mem
{
    public const int ByteSize = 8;
    public static PooledBuffer CreateBuffer(int size) => new(size);

    public static T[] Rent<T>(int size) => ArrayPool<T>.Shared.Rent(size);
    public static byte[] Rent(int size) => Rent<byte>(size);

    public static void Return<T>(T[] arr, bool clearArray = false) =>
        ArrayPool<T>.Shared.Return(arr, clearArray);

    public static bool BytesEqual(ReadOnlySpan<byte> a1, ReadOnlySpan<byte> a2) =>
        a1.Length == a2.Length && a1.SequenceEqual(a2);

    public static PooledBuffer StructToBytes<T>(T message) where T : struct
    {
        var size = Marshal.SizeOf(message);
        var buffer = CreateBuffer(size);
        StructToBytes(message, buffer, size);
        return buffer;
    }

    public static void StructToBytes<T>(T message, byte[] body, int size) where T : struct
    {
        var ptr = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.StructureToPtr(message, ptr, true);
            Marshal.Copy(ptr, body, 0, size);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    public static T BytesToStruct<T>(byte[] body) where T : struct
    {
        var size = Marshal.SizeOf<T>();
        return BytesToStruct<T>(body, size);
    }


    public static T BytesToStruct<T>(byte[] body, int size) where T : struct
    {
        var ptr = Marshal.AllocHGlobal(size);

        try
        {
            Marshal.Copy(body, 0, ptr, size);
            var result = (T) Marshal.PtrToStructure(ptr, typeof(T))!;
            return result;
        }
        finally
        {
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