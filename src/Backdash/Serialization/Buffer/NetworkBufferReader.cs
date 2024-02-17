using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Backdash.Network;

namespace Backdash.Serialization.Buffer;

public readonly ref struct BinaryBufferReader
{
    public BinaryBufferReader(ReadOnlySpan<byte> buffer) => this.buffer = buffer;

    public BinaryBufferReader(ReadOnlySpan<byte> buffer, ref int offset) : this(buffer) =>
        this.offset = ref offset;

    readonly ref int offset;
    readonly ReadOnlySpan<byte> buffer;

    public bool Network { get; init; } = true;

    public int ReadCount => offset;
    public int Capacity => buffer.Length;

    int FreeCapacity => Capacity - ReadCount;

    public ReadOnlySpan<byte> CurrentBuffer => buffer[offset..];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count) => offset += count;

    const int FullSize = -1;

    void ReadSpan<T>(in Span<T> data, int size) where T : struct
    {
        var sliceSize = size < 0 ? data.Length : size;
        ReadByte(MemoryMarshal.AsBytes(data[..sliceSize]));
    }

    public byte ReadByte() => buffer[offset++];

    public void ReadByte(in Span<byte> data, int size = FullSize)
    {
        var length = size < 0 ? data.Length : size;

        if (length > FreeCapacity)
            throw new InvalidOperationException("Not available buffer space");

        var slice = buffer.Slice(offset, length);
        Advance(length);
        slice.CopyTo(data[..length]);
    }

    public sbyte ReadSByte() => unchecked((sbyte)buffer[offset++]);

    public void ReadSByte(in Span<sbyte> values, int size = FullSize) => ReadSpan(values, size);

    public bool ReadBool()
    {
        var value = BitConverter.ToBoolean(CurrentBuffer);
        Advance(sizeof(bool));
        return value;
    }

    public void ReadBool(in Span<bool> values, int size = FullSize) => ReadSpan(values, size);

    public short ReadShort()
    {
        var value = BitConverter.ToInt16(CurrentBuffer);
        Advance(sizeof(short));
        return Network ? Endianness.ToHost(in value) : value;
    }

    public void ReadShort(in Span<short> values, int size = FullSize)
    {
        ReadSpan(values, size);
        if (Network) Endianness.ToHost(values);
    }

    public ushort ReadUShort()
    {
        var value = BitConverter.ToUInt16(CurrentBuffer);
        Advance(sizeof(ushort));

        return Network ? Endianness.ToHost(in value) : value;
    }

    public void ReadUShort(in Span<ushort> values, int size = FullSize)
    {
        ReadSpan(values, size);
        if (Network) Endianness.ToHost(values);
    }

    public char ReadChar()
    {
        var value = BitConverter.ToChar(CurrentBuffer);
        Advance(sizeof(char));
        return Network ? Endianness.ToHost(in value) : value;
    }

    public void ReadChar(in Span<char> values, int size = FullSize)
    {
        ReadSpan(values, size);
        if (Network) Endianness.ToHost(values);
    }

    public int ReadInt()
    {
        var value = BitConverter.ToInt32(CurrentBuffer);
        Advance(sizeof(int));
        return Network ? Endianness.ToHost(in value) : value;
    }

    public void ReadInt(in Span<int> values, int size = FullSize)
    {
        ReadSpan(values, size);
        if (Network) Endianness.ToHost(values);
    }

    public uint ReadUInt()
    {
        var value = BitConverter.ToUInt32(CurrentBuffer);
        Advance(sizeof(uint));

        return Network ? Endianness.ToHost(in value) : value;
    }

    public void ReadUInt(in Span<uint> values, int size = FullSize)
    {
        ReadSpan(values, size);
        if (Network) Endianness.ToHost(values);
    }

    public long ReadLong()
    {
        var value = BitConverter.ToInt64(CurrentBuffer);
        Advance(sizeof(long));
        return Network ? Endianness.ToHost(in value) : value;
    }

    public void ReadLong(in Span<long> values, int size = FullSize)
    {
        ReadSpan(values, size);
        if (Network) Endianness.ToHost(values);
    }

    public ulong ReadULong()
    {
        var value = BitConverter.ToUInt64(CurrentBuffer);
        Advance(sizeof(ulong));

        return Network ? Endianness.ToHost(in value) : value;
    }

    public void ReadULong(in Span<ulong> values, int size = FullSize)
    {
        ReadSpan(values, size);
        if (Network) Endianness.ToHost(values);
    }

    public Int128 ReadInt128()
    {
        var value = ToInt128(CurrentBuffer);
        Advance(Unsafe.SizeOf<Int128>());

        return Network ? Endianness.ToHost(in value) : value;

        static Int128 ToInt128(ReadOnlySpan<byte> value)
        {
            if (value.Length < Unsafe.SizeOf<Int128>())
                throw new ArgumentOutOfRangeException(nameof(value));

            return Unsafe.ReadUnaligned<Int128>(ref MemoryMarshal.GetReference(value));
        }
    }

    public void ReadInt128(in Span<Int128> values, int size = FullSize)
    {
        ReadSpan(values, size);
        if (Network) Endianness.ToHost(values);
    }

    public UInt128 ReadUInt128()
    {
        var value = ToUInt128(CurrentBuffer);
        Advance(Unsafe.SizeOf<UInt128>());

        return Network ? Endianness.ToHost(in value) : value;

        static UInt128 ToUInt128(ReadOnlySpan<byte> value)
        {
            if (value.Length < Unsafe.SizeOf<UInt128>())
                throw new ArgumentOutOfRangeException(nameof(value));

            return Unsafe.ReadUnaligned<UInt128>(ref MemoryMarshal.GetReference(value));
        }
    }

    public void ReadUInt128(in Span<UInt128> values, int size = FullSize)
    {
        ReadSpan(values, size);
        if (Network) Endianness.ToHost(values);
    }
}
