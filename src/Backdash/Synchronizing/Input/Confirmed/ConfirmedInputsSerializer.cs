using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Backdash.Network;
using Backdash.Serialization;

namespace Backdash.Synchronizing.Input.Confirmed;

sealed class ConfirmedInputsSerializer<T>(IBinarySerializer<T> inputSerializer) : IBinarySerializer<ConfirmedInputs<T>>
    where T : unmanaged
{
    readonly Endianness endianness = inputSerializer.Endianness;

    /// <inheritdoc cref="NetcodeOptions.UseNetworkEndianness"/>
    public Endianness Endianness => endianness;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Write(in BinaryRawBufferWriter writer, in ConfirmedInputs<T> data)
    {
        ReadOnlySpan<T> inputs = data.Inputs;
        writer.Write(data.Count);

        ref var current = ref MemoryMarshal.GetReference(inputs);
        ref var limit = ref Unsafe.Add(ref current, data.Count);
        while (Unsafe.IsAddressLessThan(ref current, ref limit))
        {
            var size = inputSerializer.Serialize(in current, writer.CurrentBuffer);
            writer.Advance(size);

            current = ref Unsafe.Add(ref current, 1)!;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Read(in BinaryBufferReader reader, ref ConfirmedInputs<T> result)
    {
        Span<T> inputs = result.Inputs;
        result.Count = reader.ReadByte();

        ref var current = ref MemoryMarshal.GetReference(inputs);
        ref var limit = ref Unsafe.Add(ref current, result.Count);
        while (Unsafe.IsAddressLessThan(ref current, ref limit))
        {
            var size = inputSerializer.Deserialize(reader.CurrentBuffer, ref current);
            reader.Advance(size);

            current = ref Unsafe.Add(ref current, 1)!;
        }
    }

    public int Serialize(in ConfirmedInputs<T> data, Span<byte> buffer)
    {
        var offset = 0;
        BinaryRawBufferWriter writer = new(buffer, ref offset, endianness);
        Write(in writer, in data);
        return writer.WrittenCount;
    }

    public int Deserialize(ReadOnlySpan<byte> data, ref ConfirmedInputs<T> value)
    {
        var offset = 0;
        BinaryBufferReader reader = new(data, ref offset, endianness);
        Read(in reader, ref value);
        return offset;
    }
}
