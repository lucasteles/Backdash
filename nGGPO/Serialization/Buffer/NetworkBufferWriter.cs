using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using nGGPO.Network;

namespace nGGPO.Serialization.Buffer;

public ref struct NetworkBufferWriter
{
    int offset;
    readonly Span<byte> buffer;

    public bool Network { get; init; }
    public int WrittenCount => offset;
    public int Capacity => buffer.Length;
    public int FreeCapacity => Capacity - WrittenCount;

    public NetworkBufferWriter(Span<byte> buffer, int offset = 0)
    {
        this.buffer = buffer;
        this.offset = offset;
    }

    public void Advance(int count) => offset += count;

    void WriteSpan<T>(in ReadOnlySpan<T> data) where T : struct =>
        Write(MemoryMarshal.AsBytes(data));

    Span<T> GetSpanFor<T>(in ReadOnlySpan<T> value) where T : struct
    {
        var valueBytes = MemoryMarshal.AsBytes(value);
        return MemoryMarshal.Cast<byte, T>(buffer[offset..valueBytes.Length]);
    }

    public void Write(byte value) => buffer[offset++] = value;

    public void Write(in ReadOnlySpan<byte> value)
    {
        value.CopyTo(buffer[offset..]);
        offset += value.Length;
    }

    public void Write(sbyte value) => buffer[offset++] = unchecked((byte) value);

    public void Write(in ReadOnlySpan<sbyte> value) => WriteSpan(value);

    public void Write(bool value)
    {
        BitConverter.TryWriteBytes(buffer[offset..], value).AssertTrue();
        offset += sizeof(bool);
    }

    public void Write(in ReadOnlySpan<bool> value) => WriteSpan(value);

    public void Write(short value)
    {
        var reordered = Network ? Endianness.ToNetwork(value) : value;
        BitConverter.TryWriteBytes(buffer[offset..], reordered).AssertTrue();
        offset += sizeof(short);
    }

    public void Write(in ReadOnlySpan<short> value)
    {
        if (Network)
            Endianness.ToNetwork(value, GetSpanFor(value));
        else
            WriteSpan(value);
    }

    public void Write(int value)
    {
        var reordered = Network ? Endianness.ToNetwork(value) : value;
        BitConverter.TryWriteBytes(buffer[offset..], reordered).AssertTrue();
        offset += sizeof(int);
    }

    public void Write(in ReadOnlySpan<int> value)
    {
        if (Network)
            Endianness.ToNetwork(value, GetSpanFor(value));
        else
            WriteSpan(value);
    }


    public void Write(long value)
    {
        var reordered = Network ? Endianness.ToNetwork(value) : value;
        BitConverter.TryWriteBytes(buffer[offset..], reordered).AssertTrue();
        offset += sizeof(long);
    }

    public void Write(in ReadOnlySpan<long> value)
    {
        if (Network)
            Endianness.ToNetwork(value, GetSpanFor(value));
        else
            WriteSpan(value);
    }

    public void Write(char value)
    {
        var reordered = Network ? Endianness.ToNetwork(value) : value;
        BitConverter.TryWriteBytes(buffer[offset..], reordered).AssertTrue();
        offset += sizeof(char);
    }

    public void Write(in ReadOnlySpan<char> value)
    {
        if (Network)
            Endianness.ToNetwork(value, GetSpanFor(value));
        else
            WriteSpan(value);
    }

    public void Write(uint value)
    {
        var reordered = Network ? Endianness.ToNetwork(value) : value;
        BitConverter.TryWriteBytes(buffer[offset..], reordered).AssertTrue();
        offset += sizeof(uint);
    }

    public void Write(in ReadOnlySpan<uint> value)
    {
        if (Network)
            Endianness.ToNetwork(value, GetSpanFor(value));
        else
            WriteSpan(value);
    }

    public void Write(ushort value)
    {
        var reordered = Network ? Endianness.ToNetwork(value) : value;
        BitConverter.TryWriteBytes(buffer[offset..], reordered).AssertTrue();
        offset += sizeof(ushort);
    }

    public void Write(in ReadOnlySpan<ushort> value)
    {
        if (Network)
            Endianness.ToNetwork(value, GetSpanFor(value));
        else
            WriteSpan(value);
    }

    public void Write(ulong value)
    {
        var reordered = Network ? Endianness.ToNetwork(value) : value;
        BitConverter.TryWriteBytes(buffer[offset..], reordered).AssertTrue();
        offset += sizeof(ulong);
    }

    public void Write(in ReadOnlySpan<ulong> value)
    {
        if (Network)
            Endianness.ToNetwork(value, GetSpanFor(value));
        else
            WriteSpan(value);
    }

    public void Write(Memory<byte> value) => Write(value.Span);

    public void Write(Int128 value)
    {
        var reordered = Network ? Endianness.ToNetwork(value) : value;
        WriteInt128(buffer[offset..], reordered).AssertTrue();
        offset += Unsafe.SizeOf<Int128>();

        return;

        static bool WriteInt128(Span<byte> destination, Int128 value)
        {
            if (destination.Length < Unsafe.SizeOf<Int128>()) return false;
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
            return true;
        }
    }

    public void Write(in ReadOnlySpan<Int128> value)
    {
        if (Network)
            Endianness.ToNetwork(value, GetSpanFor(value));
        else
            WriteSpan(value);
    }

    public void Write(UInt128 value)
    {
        var reordered = Network ? Endianness.ToNetwork(value) : value;
        WriteUInt128(buffer[offset..], reordered).AssertTrue();
        offset += Unsafe.SizeOf<UInt128>();

        return;

        static bool WriteUInt128(Span<byte> destination, UInt128 value)
        {
            if (destination.Length < Unsafe.SizeOf<UInt128>()) return false;
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
            return true;
        }
    }

    public void Write(in ReadOnlySpan<UInt128> value)
    {
        if (Network)
            Endianness.ToNetwork(value, GetSpanFor(value));
        else
            WriteSpan(value);
    }
}