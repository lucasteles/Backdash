using System.Runtime.CompilerServices;
using Backdash.Core;
using Backdash.Network;

namespace Backdash.Serialization.Internal;

sealed class StructBinarySerializer<T> : IBinarySerializer<T> where T : struct
{
    public Endianness Endianness { get; } = Platform.GetEndianness(false);

    public int Serialize(in T data, Span<byte> buffer) => Mem.WriteStruct(in data, buffer);

    public int Deserialize(ReadOnlySpan<byte> data, ref T value)
    {
        value = Mem.ReadStruct<T>(in data);
        return Unsafe.SizeOf<T>();
    }
}
