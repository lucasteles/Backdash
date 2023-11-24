using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using nGGPO.Network;

namespace nGGPO.Serialization.Buffer;

public ref struct NetworkBufferReader
{
    int offset;
    readonly ReadOnlySpan<byte> buffer;
    readonly bool network;

    public int ReadCount => offset;
    public int Capacity => buffer.Length;
    public int FreeCapacity => Capacity - ReadCount;

    public ReadOnlySpan<byte> CurrentBuffer => buffer[offset..];

    public NetworkBufferReader(ReadOnlySpan<byte> buffer, bool network = true, int offset = 0)
    {
        this.buffer = buffer;
        this.network = network;
        this.offset = offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count) => offset += count;

    void ReadSpan<T>(in Span<T> data) where T : struct =>
        ReadByte(MemoryMarshal.AsBytes(data));

    public byte ReadByte() => buffer[offset++];

    public void ReadByte(in Span<byte> data)
    {
        var size = data.Length;
        var slice = buffer.Slice(offset, size);
        Advance(size);
        slice.CopyTo(data);
    }

    public sbyte ReadSByte() => unchecked((sbyte) buffer[offset++]);

    public void ReadSByte(in Span<sbyte> values) => ReadSpan(values);

    public bool ReadBool()
    {
        var value = BitConverter.ToBoolean(CurrentBuffer);
        Advance(sizeof(bool));
        return value;
    }

    public void ReadBool(in Span<bool> values) => ReadSpan(values);

    public short ReadShort()
    {
        var value = BitConverter.ToInt16(CurrentBuffer);
        Advance(sizeof(short));
        return network ? Endianness.ToHost(value) : value;
    }

    public void ReadShort(in Span<short> values)
    {
        ReadSpan(values);
        if (network) Endianness.ToHost(values);
    }

    public ushort ReadUShort()
    {
        var value = BitConverter.ToUInt16(CurrentBuffer);
        Advance(sizeof(ushort));

        return network ? Endianness.ToHost(value) : value;
    }

    public void ReadUShort(in Span<ushort> values)
    {
        ReadSpan(values);
        if (network) Endianness.ToHost(values);
    }

    public char ReadChar()
    {
        var value = BitConverter.ToChar(CurrentBuffer);
        Advance(sizeof(char));
        return network ? Endianness.ToHost(value) : value;
    }

    public void ReadChar(in Span<char> values)
    {
        ReadSpan(values);
        if (network) Endianness.ToHost(values);
    }

    public int ReadInt()
    {
        var value = BitConverter.ToInt32(CurrentBuffer);
        Advance(sizeof(int));
        return network ? Endianness.ToHost(value) : value;
    }

    public void ReadInt(in Span<int> values)
    {
        ReadSpan(values);
        if (network) Endianness.ToHost(values);
    }

    public uint ReadUInt()
    {
        var value = BitConverter.ToUInt32(CurrentBuffer);
        Advance(sizeof(uint));

        return network ? Endianness.ToHost(value) : value;
    }

    public void ReadUInt(in Span<uint> values)
    {
        ReadSpan(values);
        if (network) Endianness.ToHost(values);
    }

    public long ReadLong()
    {
        var value = BitConverter.ToInt64(CurrentBuffer);
        Advance(sizeof(long));
        return network ? Endianness.ToHost(value) : value;
    }

    public void ReadLong(in Span<long> values)
    {
        ReadSpan(values);
        if (network) Endianness.ToHost(values);
    }

    public ulong ReadULong()
    {
        var value = BitConverter.ToUInt64(CurrentBuffer);
        Advance(sizeof(ulong));

        return network ? Endianness.ToHost(value) : value;
    }

    public void ReadULong(in Span<ulong> values)
    {
        ReadSpan(values);
        if (network) Endianness.ToHost(values);
    }

    public Int128 ReadInt128()
    {
        var value = ToInt128(CurrentBuffer);
        Advance(Unsafe.SizeOf<Int128>());

        return network ? Endianness.ToHost(value) : value;

        static Int128 ToInt128(ReadOnlySpan<byte> value)
        {
            if (value.Length < Unsafe.SizeOf<Int128>())
                throw new ArgumentOutOfRangeException(nameof(value));

            return Unsafe.ReadUnaligned<Int128>(ref MemoryMarshal.GetReference(value));
        }
    }

    public void ReadInt128(in Span<Int128> values)
    {
        ReadSpan(values);
        if (network) Endianness.ToHost(values);
    }

    public UInt128 ReadUInt128()
    {
        var value = ToUInt128(CurrentBuffer);
        Advance(Unsafe.SizeOf<UInt128>());

        return network ? Endianness.ToHost(value) : value;

        static UInt128 ToUInt128(ReadOnlySpan<byte> value)
        {
            if (value.Length < Unsafe.SizeOf<UInt128>())
                throw new ArgumentOutOfRangeException(nameof(value));

            return Unsafe.ReadUnaligned<UInt128>(ref MemoryMarshal.GetReference(value));
        }
    }

    public void ReadUInt128(in Span<UInt128> values)
    {
        ReadSpan(values);
        if (network) Endianness.ToHost(values);
    }


    // public TEnum ReadEnum<TEnum, TUnderType>()
    //     where TEnum : unmanaged, Enum
    //     where TUnderType : unmanaged, IBinaryInteger<TUnderType>
    // {
    //     var size = Unsafe.SizeOf<TUnderType>();
    //     var value = Mem.SpanAsStruct<TUnderType>(buffer[offset..(offset + size)]);
    //     Advance( size);
    //     var reordered = network ? Endianness.TryNetworkToHostOrder(value) : value;
    //     return Mem.IntegerAsEnum<TEnum, TUnderType>(reordered);
    // }
}