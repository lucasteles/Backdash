using System;
using System.Buffers;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace nGGPO;

public readonly struct ScopedBuffer : IDisposable
{
    public readonly byte[] Bytes;
    readonly bool pooled;

    public ScopedBuffer(int size)
    {
        Bytes = Mem.Rent<byte>(size);
        pooled = true;
    }

    public ScopedBuffer(byte[] bytes)
    {
        Bytes = bytes;
        pooled = false;
    }

    public void Dispose()
    {
        if (pooled)
            Mem.Return(Bytes, true);
    }

    public static implicit operator byte[](ScopedBuffer @this) => @this.Bytes;
    public static implicit operator ReadOnlySpan<byte>(ScopedBuffer @this) => @this.Bytes;
    public static implicit operator Span<byte>(ScopedBuffer @this) => @this.Bytes;
}

public static class Mem
{
    public const int ByteSize = 8;
    public static ScopedBuffer CreateBuffer(int size) => new(size);

    public static T[] Rent<T>(int size) => ArrayPool<T>.Shared.Rent(size);
    public static byte[] Rent(int size) => Rent<byte>(size);

    public static void Return<T>(T[] arr, bool clearArray = true) =>
        ArrayPool<T>.Shared.Return(arr, clearArray);

    public static bool BytesEqual(ReadOnlySpan<byte> a1, ReadOnlySpan<byte> a2) =>
        a1.Length == a2.Length && a1.SequenceEqual(a2);

    public static ScopedBuffer SerializeMarshal<T>(T message) where T : notnull
    {
        var size = Marshal.SizeOf(message);
        ScopedBuffer buffer = new(size);
        SerializeMarshal(message, buffer, size);
        return buffer;
    }

    public static void SerializeMarshal<T>(T message, byte[] body, int size) where T : notnull
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

    public static T DeserializeMarshal<T>(byte[] body) where T : notnull
    {
        var size = Marshal.SizeOf<T>();
        return DeserializeMarshal<T>(body, size);
    }


    public static T DeserializeMarshal<T>(byte[] body, int size) where T : notnull
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