using System.Runtime.CompilerServices;
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
        for (var i = 0; i < data.Count; i++)
        {
            var size = inputSerializer.Serialize(in inputs[i], writer.CurrentBuffer);
            writer.Advance(size);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void Read(in BinaryBufferReader reader, ref ConfirmedInputs<T> result)
    {
        Span<T> inputs = result.Inputs;
        result.Count = reader.ReadByte();
        for (var i = 0; i < result.Count; i++)
        {
            var size = inputSerializer.Deserialize(reader.CurrentBuffer, ref inputs[i]);
            reader.Advance(size);
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
