using System;
using System.Buffers;
using System.Runtime.InteropServices;

namespace nGGPO.Network;

public struct ByteBufferScope : IDisposable
{
    public readonly byte[] Bytes;
    readonly bool rented;

    public ByteBufferScope(bool rented, byte[] bytes)
    {
        Bytes = bytes;
        this.rented = rented;
    }

    public void Dispose()
    {
        if (rented)
            ArrayPool<byte>.Shared.Return(Bytes);
    }
}

public interface IBinaryEncoder
{
    ByteBufferScope Encode<T>(T message) where T : notnull;
    T Decode<T>(byte[] body) where T : notnull;
}

class StructBinaryEncoder : IBinaryEncoder
{
    public ByteBufferScope Encode<T>(T message) where T : notnull
    {
        var size = Marshal.SizeOf(message);
        var body = ArrayPool<byte>.Shared.Rent(size);
        Encode(message, body, size);
        return new(true, body);
    }

    public void Encode<T>(T message, byte[] body, int size) where T : notnull
    {
        var ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(message, ptr, true);
        Marshal.Copy(ptr, body, 0, size);
        Marshal.FreeHGlobal(ptr);
    }

    public T Decode<T>(byte[] body) where T : notnull
    {
        var size = Marshal.SizeOf<T>();
        var ptr = Marshal.AllocHGlobal(size);
        Marshal.Copy(body, 0, ptr, size);
        var result = (T) Marshal.PtrToStructure(ptr, typeof(T))!;
        Marshal.FreeHGlobal(ptr);
        return result;
    }
}

// public class JsonBinaryEncoder : IBinaryEncoder
// {
//     readonly JsonSerializerOptions options;
//
//     public JsonBinaryEncoder(JsonSerializerOptions options) => this.options = options;
//
//     public byte[] Encode<T>(T message) where T : notnull =>
//         JsonSerializer.SerializeToUtf8Bytes(message, options);
//
//     public void Return(in byte[] bytes)
//     {
//     }
//
//     public T Decode<T>(byte[] body) where T : notnull =>
//         JsonSerializer.Deserialize<T>(body, options)!;
// }