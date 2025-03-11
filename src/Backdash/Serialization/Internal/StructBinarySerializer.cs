using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Backdash.Network;

namespace Backdash.Serialization.Internal;

sealed class StructBinarySerializer<T> : IBinarySerializer<T> where T : unmanaged
{
    public Endianness Endianness => Platform.Endianness;

    static readonly int tSize = Unsafe.SizeOf<T>();

    public int Serialize(in T data, Span<byte> buffer)
    {
        MemoryMarshal.Write(buffer, in data);
        return tSize;
    }

    public int Deserialize(ReadOnlySpan<byte> data, ref T value)
    {
        value = MemoryMarshal.Read<T>(data);
        return tSize;
    }
}
