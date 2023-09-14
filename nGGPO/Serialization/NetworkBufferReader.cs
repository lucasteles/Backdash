using System;
using System.Buffers.Binary;
using System.Net;

namespace nGGPO;

public ref struct NetworkBufferReader
{
    int offset;
    readonly Span<byte> buffer;
    readonly bool network;

    public NetworkBufferReader(Span<byte> buffer, bool network = true, int offset = 0)
    {
        this.buffer = buffer;
        this.network = network;
        this.offset = offset;
    }

    public byte Read() => buffer[offset++];

    public void Read(in Span<byte> data)
    {
        var size = data.Length;
        var slice = buffer.Slice(offset, size);
        offset += size;
        slice.CopyTo(data);
    }

    public byte[] Read(int size)
    {
        var data = new byte[size];
        Read(data);
        return data;
    }

    public int ReadInt()
    {
        var value = BitConverter.ToInt32(buffer[offset..]);
        offset += sizeof(int);
        return network ? IPAddress.NetworkToHostOrder(value) : value;
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
        return network ? IPAddress.NetworkToHostOrder(value) : value;
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
        return network ? IPAddress.NetworkToHostOrder(value) : value;
    }

    public void ReadLong(in Span<long> values)
    {
        for (var i = 0; i < values.Length; i++)
            values[i] = ReadLong();
    }

    public char ReadChar()
    {
        var value = BitConverter.ToChar(buffer[offset..]);
        offset += sizeof(char);
        return value;
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

        return network && BitConverter.IsLittleEndian
            ? BinaryPrimitives.ReverseEndianness(value)
            : value;
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

        return network && BitConverter.IsLittleEndian
            ? BinaryPrimitives.ReverseEndianness(value)
            : value;
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

        return network && BitConverter.IsLittleEndian
            ? BinaryPrimitives.ReverseEndianness(value)
            : value;
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
}