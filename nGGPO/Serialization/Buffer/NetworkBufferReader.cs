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

    public NetworkBufferReader(ReadOnlySpan<byte> buffer, bool network = true, int offset = 0)
    {
        this.buffer = buffer;
        this.network = network;
        this.offset = offset;
    }

    public void Advance(int count) => offset += count;
    public byte ReadByte() => buffer[offset++];

    public void ReadByte(in Span<byte> data)
    {
        var size = data.Length;
        var slice = buffer.Slice(offset, size);
        offset += size;
        slice.CopyTo(data);
    }

    public int ReadInt()
    {
        var value = BitConverter.ToInt32(buffer[offset..]);
        offset += sizeof(int);
        return network ? Endianness.NetworkToHostOrder(value) : value;
    }

    public void ReadInt(in Span<int> values)
    {
        for (var i = 0; i < values.Length; i++)
            values[i] = ReadInt();
    }

    public short ReadShort()
    {
        var value = BitConverter.ToInt16(buffer[offset..]);
        offset += sizeof(short);
        return network ? Endianness.NetworkToHostOrder(value) : value;
    }

    public void ReadShort(in Span<short> values)
    {
        for (var i = 0; i < values.Length; i++)
            values[i] = ReadShort();
    }

    public long ReadLong()
    {
        var value = BitConverter.ToInt64(buffer[offset..]);
        offset += sizeof(long);
        return network ? Endianness.NetworkToHostOrder(value) : value;
    }

    public void ReadLong(in Span<long> values)
    {
        for (var i = 0; i < values.Length; i++)
            values[i] = ReadLong();
    }

    public Int128 ReadInt128()
    {
        static Int128 ToInt128(ReadOnlySpan<byte> value)
        {
            if (value.Length < Unsafe.SizeOf<Int128>())
                throw new ArgumentOutOfRangeException(nameof(value));

            return Unsafe.ReadUnaligned<Int128>(ref MemoryMarshal.GetReference(value));
        }

        var value = ToInt128(buffer[offset..]);
        offset += Unsafe.SizeOf<Int128>();

        return network ? Endianness.NetworkToHostOrder(value) : value;
    }

    public void ReadInt128(in Span<Int128> values)
    {
        for (var i = 0; i < values.Length; i++)
            values[i] = ReadInt128();
    }

    public UInt128 ReadUInt128()
    {
        var value = ToUInt128(buffer[offset..]);
        offset += Unsafe.SizeOf<UInt128>();

        return network ? Endianness.NetworkToHostOrder(value) : value;

        static UInt128 ToUInt128(ReadOnlySpan<byte> value)
        {
            if (value.Length < Unsafe.SizeOf<UInt128>())
                throw new ArgumentOutOfRangeException(nameof(value));

            return Unsafe.ReadUnaligned<UInt128>(ref MemoryMarshal.GetReference(value));
        }
    }

    public void ReadUInt128(in Span<UInt128> values)
    {
        for (var i = 0; i < values.Length; i++)
            values[i] = ReadUInt128();
    }

    public char ReadChar()
    {
        var value = BitConverter.ToChar(buffer[offset..]);
        offset += sizeof(char);
        return network ? Endianness.NetworkToHostOrder(value) : value;
    }

    public void ReadChar(in Span<char> values)
    {
        for (var i = 0; i < values.Length; i++)
            values[i] = ReadChar();
    }

    public bool ReadBool()
    {
        var value = BitConverter.ToBoolean(buffer[offset..]);
        offset += sizeof(bool);
        return value;
    }

    public void ReadBool(in Span<bool> values)
    {
        for (var i = 0; i < values.Length; i++)
            values[i] = ReadBool();
    }

    public uint ReadUInt()
    {
        var value = BitConverter.ToUInt32(buffer[offset..]);
        offset += sizeof(uint);

        return network ? Endianness.NetworkToHostOrder(value) : value;
    }

    public void ReadUInt(in Span<uint> values)
    {
        for (var i = 0; i < values.Length; i++)
            values[i] = ReadUInt();
    }

    public ushort ReadUShort()
    {
        var value = BitConverter.ToUInt16(buffer[offset..]);
        offset += sizeof(ushort);

        return network ? Endianness.NetworkToHostOrder(value) : value;
    }

    public void ReadUShort(in Span<ushort> values)
    {
        for (var i = 0; i < values.Length; i++)
            values[i] = ReadUShort();
    }

    public ulong ReadULong()
    {
        var value = BitConverter.ToUInt64(buffer[offset..]);
        offset += sizeof(ulong);

        return network ? Endianness.NetworkToHostOrder(value) : value;
    }

    public void ReadULong(in Span<ulong> values)
    {
        for (var i = 0; i < values.Length; i++)
            values[i] = ReadULong();
    }

    public sbyte ReadSByte() => unchecked((sbyte) buffer[offset++]);

    public void ReadSByte(in Span<sbyte> values)
    {
        for (var i = 0; i < values.Length; i++)
            values[i] = ReadSByte();
    }

    // public TEnum ReadEnum<TEnum, TUnderType>()
    //     where TEnum : unmanaged, Enum
    //     where TUnderType : unmanaged, IBinaryInteger<TUnderType>
    // {
    //     var size = Unsafe.SizeOf<TUnderType>();
    //     var value = Mem.SpanAsStruct<TUnderType>(buffer[offset..(offset + size)]);
    //     offset += size;
    //     var reordered = network ? Endianness.TryNetworkToHostOrder(value) : value;
    //     return Mem.IntegerAsEnum<TEnum, TUnderType>(reordered);
    // }
}