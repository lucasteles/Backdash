using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Backdash.Network;

namespace Backdash.Serialization.Buffer;

public readonly ref struct BinarySpanReader
{
    public BinarySpanReader(ReadOnlySpan<byte> buffer, ref int offset)
    {
        this.offset = ref offset;
        this.buffer = buffer;
    }

    readonly ref int offset;
    readonly ReadOnlySpan<byte> buffer;
    public int ReadCount => offset;
    public int Capacity => buffer.Length;
    int FreeCapacity => Capacity - ReadCount;
    public ReadOnlySpan<byte> CurrentBuffer => buffer[offset..];
    public Endianness Endianness { get; init; } = Endianness.BigEndian;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count) => offset += count;

    public byte ReadByte() => buffer[offset++];

    public void ReadByte(in Span<byte> data)
    {
        var length = data.Length;
        if (length > FreeCapacity)
            throw new InvalidOperationException("Not available buffer space");
        var slice = buffer.Slice(offset, length);
        Advance(length);
        slice.CopyTo(data[..length]);
    }

    void ReadSpan<T>(in Span<T> data) where T : struct => ReadByte(MemoryMarshal.AsBytes(data));
    public sbyte ReadSByte() => unchecked((sbyte)buffer[offset++]);
    public void ReadSByte(in Span<sbyte> value) => ReadSpan(value);

    public bool ReadBool()
    {
        var value = BitConverter.ToBoolean(CurrentBuffer);
        Advance(sizeof(bool));
        return value;
    }

    public void ReadBool(in Span<bool> values) => ReadSpan(values);
    public short ReadShort() => ReadNumber<short>(false);

    public void ReadShort(in Span<short> values)
    {
        ReadSpan(values);
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(values, values);
    }

    public ushort ReadUShort() => ReadNumber<ushort>(true);

    public void ReadUShort(in Span<ushort> values)
    {
        ReadSpan(values);
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(values, values);
    }

    public char ReadChar() => (char)ReadUShort();

    public void ReadChar(in Span<char> values)
    {
        ReadSpan(values);
        if (Endianness != Platform.Endianness)
        {
            var ushortSpan = MemoryMarshal.Cast<char, ushort>(values);
            BinaryPrimitives.ReverseEndianness(ushortSpan, ushortSpan);
        }
    }

    public int ReadInt() => ReadNumber<int>(false);

    public void ReadInt(in Span<int> values)
    {
        ReadSpan(values);
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(values, values);
    }

    public uint ReadUInt() => ReadNumber<uint>(true);

    public void ReadUInt(in Span<uint> values)
    {
        ReadSpan(values);
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(values, values);
    }

    public long ReadLong() => ReadNumber<long>(false);

    public void ReadLong(in Span<long> values)
    {
        ReadSpan(values);
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(values, values);
    }

    public ulong ReadULong() => ReadNumber<ulong>(true);

    public void ReadULong(in Span<ulong> values)
    {
        ReadSpan(values);
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(values, values);
    }

    public Int128 ReadInt128() => ReadNumber<Int128>(false);

    public void ReadInt128(in Span<Int128> values)
    {
        ReadSpan(values);
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(values, values);
    }

    public UInt128 ReadUInt128() => ReadNumber<UInt128>(true);

    public void ReadUInt128(in Span<UInt128> values)
    {
        ReadSpan(values);
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(values, values);
    }

    public Half ReadHalf() => BitConverter.Int16BitsToHalf(ReadShort());
    public float ReadFloat() => BitConverter.Int32BitsToSingle(ReadInt());
    public double ReadDouble() => BitConverter.Int64BitsToDouble(ReadLong());

    public Vector2 ReadVector2()
    {
        var x = ReadFloat();
        var y = ReadFloat();
        return new(x, y);
    }

    public Vector3 ReadVector3()
    {
        var x = ReadFloat();
        var y = ReadFloat();
        var z = ReadFloat();
        return new(x, y, z);
    }

    public Vector4 ReadVector4()
    {
        var x = ReadFloat();
        var y = ReadFloat();
        var z = ReadFloat();
        var w = ReadFloat();
        return new(x, y, z, w);
    }

    public Quaternion ReadQuaternion()
    {
        var x = ReadFloat();
        var y = ReadFloat();
        var z = ReadFloat();
        var w = ReadFloat();
        return new Quaternion(x, y, z, w);
    }

    public T ReadNumber<T>() where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T> =>
        ReadNumber<T>(T.IsZero(T.MinValue));

    public T ReadNumber<T>(bool isUnsigned) where T : unmanaged, IBinaryInteger<T>
    {
        var size = Unsafe.SizeOf<T>();
        var result = Endianness switch
        {
            Endianness.LittleEndian => T.ReadLittleEndian(CurrentBuffer[..size], isUnsigned),
            Endianness.BigEndian => T.ReadBigEndian(CurrentBuffer[..size], isUnsigned),
            _ => default,
        };
        Advance(size);
        return result;
    }

    public T ReadEnum<T>() where T : unmanaged, Enum
    {
        switch (Type.GetTypeCode(typeof(T)))
        {
            case TypeCode.Int32:
                {
                    var value = ReadInt();
                    return Unsafe.As<int, T>(ref value);
                }
            case TypeCode.UInt32:
                {
                    var value = ReadUInt();
                    return Unsafe.As<uint, T>(ref value);
                }
            case TypeCode.Int64:
                {
                    var value = ReadLong();
                    return Unsafe.As<long, T>(ref value);
                }
            case TypeCode.UInt64:
                {
                    var value = ReadULong();
                    return Unsafe.As<ulong, T>(ref value);
                }
            case TypeCode.Int16:
                {
                    var value = ReadShort();
                    return Unsafe.As<short, T>(ref value);
                }
            case TypeCode.UInt16:
                {
                    var value = ReadUShort();
                    return Unsafe.As<ushort, T>(ref value);
                }
            case TypeCode.Byte:
                {
                    var value = ReadByte();
                    return Unsafe.As<byte, T>(ref value);
                }
            case TypeCode.SByte:
                {
                    var value = ReadSByte();
                    return Unsafe.As<sbyte, T>(ref value);
                }
            default: throw new InvalidOperationException("Unknown enum underlying type");
        }
    }
}
