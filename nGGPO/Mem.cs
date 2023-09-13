using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace nGGPO;

public readonly struct ScopedBuffer : IDisposable
{
    public readonly byte[] Bytes;

    public ScopedBuffer(int size)
    {
        Bytes = ArrayPool<byte>.Shared.Rent(size);
    }

    public void Dispose() => ArrayPool<byte>.Shared.Return(Bytes);

    public static implicit operator byte[](ScopedBuffer @this) => @this.Bytes;
    public static implicit operator ReadOnlySpan<byte>(ScopedBuffer @this) => @this.Bytes;
    public static implicit operator ArraySegment<byte>(ScopedBuffer @this) => @this.Bytes;
    public static implicit operator Span<byte>(ScopedBuffer @this) => @this.Bytes;
}

public static class Mem
{
    public static ScopedBuffer CreateBuffer(int size) => new(size);

    public static bool BytesEqual(ReadOnlySpan<byte> a1, ReadOnlySpan<byte> a2) =>
        a1.SequenceEqual(a2);

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
}