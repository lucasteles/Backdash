using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using nGGPO.Network;

namespace nGGPO.Serialization.Buffer;

public ref struct NetworkBufferWriter
{
    readonly ref int offset;
    readonly Span<byte> buffer;

    public bool Network { get; init; } = true;
    public int WrittenCount => offset;
    public int Capacity => buffer.Length;
    public int FreeCapacity => Capacity - WrittenCount;

    public Span<byte> CurrentBuffer => buffer[offset..];

    public NetworkBufferWriter(Span<byte> buffer) => this.buffer = buffer;

    public NetworkBufferWriter(Span<byte> buffer, ref int offset) : this(buffer) =>
        this.offset = ref offset;


    const int FullSize = -1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count) => offset += count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteSpan<T>(in ReadOnlySpan<T> data, int size) where T : struct
    {
        var length = size < 0 ? data.Length : size;
        Write(MemoryMarshal.AsBytes(data[..length]));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Span<T> GetSpanFor<T>(in ReadOnlySpan<T> value, int size) where T : struct
    {
        var sliceSize = size < 0 ? value.Length : size;
        var sizeBytes = Unsafe.SizeOf<T>() * sliceSize;
        return MemoryMarshal.Cast<byte, T>(buffer[offset..sizeBytes]);
    }

    public void Write(byte value) => buffer[offset++] = value;

    public void Write(in ReadOnlySpan<byte> value, int size = FullSize)
    {
        var sliceSize = size < 0 ? value.Length : size;
        value[..sliceSize].CopyTo(CurrentBuffer);
        Advance(sliceSize);
    }

    public void Write(sbyte value) => buffer[offset++] = unchecked((byte) value);

    public void Write(in ReadOnlySpan<sbyte> value, int size = FullSize) => WriteSpan(value, size);

    public void Write(bool value)
    {
        BitConverter.TryWriteBytes(CurrentBuffer, value).AssertTrue();
        Advance(sizeof(bool));
    }

    public void Write(in ReadOnlySpan<bool> value, int size = FullSize) => WriteSpan(value, size);

    public void Write(short value)
    {
        var reordered = Network ? Endianness.ToNetwork(value) : value;
        BitConverter.TryWriteBytes(CurrentBuffer, reordered).AssertTrue();
        Advance(sizeof(short));
    }

    public void Write(in ReadOnlySpan<short> value, int size = FullSize)
    {
        if (Network)
            Endianness.ToNetwork(value, GetSpanFor(value, size));
        else
            WriteSpan(value, size);
    }

    public void Write(int value)
    {
        var reordered = Network ? Endianness.ToNetwork(value) : value;
        BitConverter.TryWriteBytes(CurrentBuffer, reordered).AssertTrue();
        Advance(sizeof(int));
    }

    public void Write(in ReadOnlySpan<int> value, int size = FullSize)
    {
        if (Network)
            Endianness.ToNetwork(value, GetSpanFor(value, size));
        else
            WriteSpan(value, size);
    }


    public void Write(long value)
    {
        var reordered = Network ? Endianness.ToNetwork(value) : value;
        BitConverter.TryWriteBytes(CurrentBuffer, reordered).AssertTrue();
        Advance(sizeof(long));
    }

    public void Write(in ReadOnlySpan<long> value, int size = FullSize)
    {
        if (Network)
            Endianness.ToNetwork(value, GetSpanFor(value, size));
        else
            WriteSpan(value, size);
    }

    public void Write(char value)
    {
        var reordered = Network ? Endianness.ToNetwork(value) : value;
        BitConverter.TryWriteBytes(CurrentBuffer, reordered).AssertTrue();
        Advance(sizeof(char));
    }

    public void Write(in ReadOnlySpan<char> value, int size = FullSize)
    {
        if (Network)
            Endianness.ToNetwork(value, GetSpanFor(value, size));
        else
            WriteSpan(value, size);
    }

    public void Write(uint value)
    {
        var reordered = Network ? Endianness.ToNetwork(value) : value;
        BitConverter.TryWriteBytes(CurrentBuffer, reordered).AssertTrue();
        Advance(sizeof(uint));
    }

    public void Write(in ReadOnlySpan<uint> value, int size = FullSize)
    {
        if (Network)
            Endianness.ToNetwork(value, GetSpanFor(value, size));
        else
            WriteSpan(value, size);
    }

    public void Write(ushort value)
    {
        var reordered = Network ? Endianness.ToNetwork(value) : value;
        BitConverter.TryWriteBytes(CurrentBuffer, reordered).AssertTrue();
        Advance(sizeof(ushort));
    }

    public void Write(in ReadOnlySpan<ushort> value, int size = FullSize)
    {
        if (Network)
            Endianness.ToNetwork(value, GetSpanFor(value, size));
        else
            WriteSpan(value, size);
    }

    public void Write(ulong value)
    {
        var reordered = Network ? Endianness.ToNetwork(value) : value;
        BitConverter.TryWriteBytes(CurrentBuffer, reordered).AssertTrue();
        Advance(sizeof(ulong));
    }

    public void Write(in ReadOnlySpan<ulong> value, int size = FullSize)
    {
        if (Network)
            Endianness.ToNetwork(value, GetSpanFor(value, size));
        else
            WriteSpan(value, size);
    }

    public void Write(Memory<byte> value) => Write(value.Span);

    public void Write(Int128 value)
    {
        var reordered = Network ? Endianness.ToNetwork(value) : value;
        WriteInt128(CurrentBuffer, reordered).AssertTrue();
        Advance(Unsafe.SizeOf<Int128>());

        return;

        static bool WriteInt128(Span<byte> destination, Int128 value)
        {
            if (destination.Length < Unsafe.SizeOf<Int128>()) return false;
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
            return true;
        }
    }

    public void Write(in ReadOnlySpan<Int128> value, int size = FullSize)
    {
        if (Network)
            Endianness.ToNetwork(value, GetSpanFor(value, size));
        else
            WriteSpan(value, size);
    }

    public void Write(UInt128 value)
    {
        var reordered = Network ? Endianness.ToNetwork(value) : value;
        WriteUInt128(CurrentBuffer, reordered).AssertTrue();
        Advance(Unsafe.SizeOf<UInt128>());

        return;

        static bool WriteUInt128(Span<byte> destination, UInt128 value)
        {
            if (destination.Length < Unsafe.SizeOf<UInt128>()) return false;
            Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(destination), value);
            return true;
        }
    }

    public void Write(in ReadOnlySpan<UInt128> value, int size = FullSize)
    {
        if (Network)
            Endianness.ToNetwork(value, GetSpanFor(value, size));
        else
            WriteSpan(value, size);
    }
}