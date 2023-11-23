using System;
using nGGPO.Network;

namespace nGGPO.Serialization.Buffer;

public ref struct NetworkBufferWriter
{
    int offset;

    readonly Span<byte> buffer;
    readonly bool network;

    public int WrittenCount => offset;
    public int Capacity => buffer.Length;
    public int FreeCapacity => Capacity - WrittenCount;
    
    public NetworkBufferWriter(Span<byte> buffer, bool network = true, int offset = 0)
    {
        this.buffer = buffer;
        this.network = network;
        this.offset = offset;
    }

    public void Advance(int count) => offset += count;

    public void Write(byte value) => buffer[offset++] = value;

    public void Write(in ReadOnlySpan<byte> value)
    {
        value.CopyTo(buffer[offset..]);
        offset += value.Length;
    }

    public void Write(sbyte value) => buffer[offset++] = unchecked((byte) value);

    public void Write(in ReadOnlySpan<sbyte> value)
    {
        for (var i = 0; i < value.Length; i++)
            Write(value[i]);
    }

    public void Write(int value)
    {
        var reordered = network ? Endianness.HostToNetworkOrder(value) : value;
        BitConverter.TryWriteBytes(buffer[offset..], reordered).AssertTrue();
        offset += sizeof(int);
    }

    public void Write(in ReadOnlySpan<int> value)
    {
        for (var i = 0; i < value.Length; i++)
            Write(value[i]);
    }

    public void Write(short value)
    {
        var reordered = network ? Endianness.HostToNetworkOrder(value) : value;
        BitConverter.TryWriteBytes(buffer[offset..], reordered).AssertTrue();
        offset += sizeof(short);
    }

    public void Write(in ReadOnlySpan<short> value)
    {
        for (var i = 0; i < value.Length; i++)
            Write(value[i]);
    }

    public void Write(long value)
    {
        var reordered = network ? Endianness.HostToNetworkOrder(value) : value;
        BitConverter.TryWriteBytes(buffer[offset..], reordered).AssertTrue();
        offset += sizeof(long);
    }

    public void Write(in ReadOnlySpan<long> value)
    {
        for (var i = 0; i < value.Length; i++)
            Write(value[i]);
    }

    public void Write(char value)
    {
        BitConverter.TryWriteBytes(buffer[offset..], value).AssertTrue();
        offset += sizeof(char);
    }

    public void Write(in ReadOnlySpan<char> value)
    {
        for (var i = 0; i < value.Length; i++)
            Write(value[i]);
    }

    public void Write(bool value)
    {
        BitConverter.TryWriteBytes(buffer[offset..], value).AssertTrue();
        offset += sizeof(bool);
    }

    public void Write(in ReadOnlySpan<bool> value)
    {
        for (var i = 0; i < value.Length; i++)
            Write(value[i]);
    }

    public void Write(uint value)
    {
        var reordered = network ? Endianness.HostToNetworkOrder(value) : value;
        BitConverter.TryWriteBytes(buffer[offset..], reordered).AssertTrue();
        offset += sizeof(uint);
    }

    public void Write(in ReadOnlySpan<uint> value)
    {
        for (var i = 0; i < value.Length; i++)
            Write(value[i]);
    }

    public void Write(ushort value)
    {
        var reordered = network ? Endianness.HostToNetworkOrder(value) : value;
        BitConverter.TryWriteBytes(buffer[offset..], reordered).AssertTrue();
        offset += sizeof(ushort);
    }

    public void Write(in ReadOnlySpan<ushort> value)
    {
        for (var i = 0; i < value.Length; i++)
            Write(value[i]);
    }

    public void Write(ulong value)
    {
        var reordered = network ? Endianness.HostToNetworkOrder(value) : value;
        BitConverter.TryWriteBytes(buffer[offset..], reordered).AssertTrue();
        offset += sizeof(ulong);
    }

    public void Write(in ReadOnlySpan<ulong> value)
    {
        for (var i = 0; i < value.Length; i++)
            Write(value[i]);
    }

    public void Write(Memory<byte> value) => Write(value.Span);
}