using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Backdash.Network;

namespace Backdash.Serialization.Buffer;

/// <summary>
/// Binary span reader.
/// </summary>
public readonly ref struct BinarySpanReader
{
    /// <summary>
    /// Initialize a new <see cref="BinarySpanReader"/> for <paramref name="buffer"/>
    /// </summary>
    /// <param name="buffer">Byte buffer to be read</param>
    /// <param name="offset">Read offset reference</param>
    public BinarySpanReader(ReadOnlySpan<byte> buffer, ref int offset)
    {
        this.offset = ref offset;
        this.buffer = buffer;
    }

    readonly ref int offset;
    readonly ReadOnlySpan<byte> buffer;

    /// <summary>
    /// Gets or init the value to define which endianness should be used for serialization.
    /// </summary>
    public Endianness Endianness { get; init; } = Endianness.BigEndian;

    /// <summary>Total read byte count.</summary>
    public int ReadCount => offset;

    /// <summary>Total buffer capacity in bytes.</summary>
    public int Capacity => buffer.Length;

    /// <summary>Available buffer space in bytes</summary>
    public int FreeCapacity => Capacity - ReadCount;

    /// <summary>Returns a <see cref="Span{Byte}"/> for the current available buffer.</summary>
    public ReadOnlySpan<byte> CurrentBuffer => buffer[offset..];


    /// <summary>Advance read pointer by <paramref name="count"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count) => offset += count;

    void ReadSpan<T>(in Span<T> data) where T : struct => ReadByte(MemoryMarshal.AsBytes(data));

    /// <summary>Reads single <see cref="byte"/> from buffer.</summary>
    public byte ReadByte() => buffer[offset++];

    /// <summary>Reads a span of <see cref="byte"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadByte(in Span<byte> values)
    {
        var length = values.Length;
        if (length > FreeCapacity)
            throw new InvalidOperationException("Not available buffer space");
        var slice = buffer.Slice(offset, length);
        Advance(length);
        slice.CopyTo(values[..length]);
    }

    /// <summary>Reads single <see cref="sbyte"/> from buffer.</summary>
    public sbyte ReadSByte() => unchecked((sbyte)buffer[offset++]);

    /// <summary>Reads a span of <see cref="sbyte"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadSByte(in Span<sbyte> values) => ReadSpan(values);

    /// <summary>Reads single <see cref="bool"/> from buffer.</summary>
    public bool ReadBoolean()
    {
        var value = BitConverter.ToBoolean(CurrentBuffer);
        Advance(sizeof(bool));
        return value;
    }

    /// <summary>Reads a span of <see cref="bool"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadBoolean(in Span<bool> values) => ReadSpan(values);

    /// <summary>Reads single <see cref="short"/> from buffer.</summary>
    public short ReadInt16() => ReadNumber<short>(false);

    /// <summary>Reads a span of <see cref="short"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadInt16(in Span<short> values)
    {
        ReadSpan(values);
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(values, values);
    }

    /// <summary>Reads single <see cref="ushort"/> from buffer.</summary>
    public ushort ReadUInt16() => ReadNumber<ushort>(true);

    /// <summary>Reads a span of <see cref="ushort"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadUInt16(in Span<ushort> values)
    {
        ReadSpan(values);
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(values, values);
    }

    /// <summary>Reads single <see cref="char"/> from buffer.</summary>
    public char ReadChar() => (char)ReadUInt16();

    /// <summary>Reads a span of <see cref="char"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadChar(in Span<char> values)
    {
        ReadSpan(values);
        if (Endianness != Platform.Endianness)
        {
            var ushortSpan = MemoryMarshal.Cast<char, ushort>(values);
            BinaryPrimitives.ReverseEndianness(ushortSpan, ushortSpan);
        }
    }

    /// <summary>Reads single <see cref="int"/> from buffer.</summary>
    public int ReadInt32() => ReadNumber<int>(false);

    /// <summary>Reads a span of <see cref="int"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadInt32(in Span<int> values)
    {
        ReadSpan(values);
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(values, values);
    }

    /// <summary>Reads single <see cref="uint"/> from buffer.</summary>
    public uint ReadUInt32() => ReadNumber<uint>(true);

    /// <summary>Reads a span of <see cref="uint"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadUInt32(in Span<uint> values)
    {
        ReadSpan(values);
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(values, values);
    }

    /// <summary>Reads single <see cref="long"/> from buffer.</summary>
    public long ReadInt64() => ReadNumber<long>(false);

    /// <summary>Reads a span of <see cref="long"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadInt64(in Span<long> values)
    {
        ReadSpan(values);
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(values, values);
    }

    /// <summary>Reads single <see cref="ulong"/> from buffer.</summary>
    public ulong ReadUInt64() => ReadNumber<ulong>(true);

    /// <summary>Reads a span of <see cref="ulong"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadUInt64(in Span<ulong> values)
    {
        ReadSpan(values);
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(values, values);
    }

    /// <summary>Reads single <see cref="Int128"/> from buffer.</summary>
    public Int128 ReadInt128() => ReadNumber<Int128>(false);

    /// <summary>Reads a span of <see cref="Int128"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadInt128(in Span<Int128> values)
    {
        ReadSpan(values);
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(values, values);
    }

    /// <summary>Reads single <see cref="UInt128"/> from buffer.</summary>
    public UInt128 ReadUInt128() => ReadNumber<UInt128>(true);

    /// <summary>Reads a span of <see cref="UInt128"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadUInt128(in Span<UInt128> values)
    {
        ReadSpan(values);
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(values, values);
    }

    /// <summary>Reads single <see cref="Half"/> from buffer.</summary>
    public Half ReadHalf() => BitConverter.Int16BitsToHalf(ReadInt16());

    /// <summary>Reads single <see cref="float"/> from buffer.</summary>
    public float ReadSingle() => BitConverter.Int32BitsToSingle(ReadInt32());

    /// <summary>Reads single <see cref="double"/> from buffer.</summary>
    public double ReadDouble() => BitConverter.Int64BitsToDouble(ReadInt64());

    /// <summary>Reads single <see cref="Vector2"/> from buffer.</summary>
    public Vector2 ReadVector2()
    {
        var x = ReadSingle();
        var y = ReadSingle();
        return new(x, y);
    }

    /// <summary>Reads single <see cref="Vector3"/> from buffer.</summary>
    public Vector3 ReadVector3()
    {
        var x = ReadSingle();
        var y = ReadSingle();
        var z = ReadSingle();
        return new(x, y, z);
    }

    /// <summary>Reads single <see cref="Vector4"/> from buffer.</summary>
    public Vector4 ReadVector4()
    {
        var x = ReadSingle();
        var y = ReadSingle();
        var z = ReadSingle();
        var w = ReadSingle();
        return new(x, y, z, w);
    }

    /// <summary>Reads single <see cref="Quaternion"/> from buffer.</summary>
    public Quaternion ReadQuaternion()
    {
        var x = ReadSingle();
        var y = ReadSingle();
        var z = ReadSingle();
        var w = ReadSingle();
        return new Quaternion(x, y, z, w);
    }

    /// <summary>Reads single <see cref="IBinaryInteger{T}"/> from buffer.</summary>
    /// <typeparam name="T">A numeric type that implements <see cref="IBinaryInteger{T}"/> and <see cref="IMinMaxValue{T}"/>.</typeparam>
    public T ReadNumber<T>() where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T> =>
        ReadNumber<T>(T.IsZero(T.MinValue));

    /// <summary>Reads single <see cref="IBinaryInteger{T}"/> from buffer.</summary>
    /// <typeparam name="T">A numeric type that implements <see cref="IBinaryInteger{T}"/>.</typeparam>
    /// <param name="isUnsigned">true if source represents an unsigned two's complement number; otherwise, false to indicate it represents a signed two's complement number</param>
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

    /// <summary>Reads single <see cref="Enum"/> value from buffer.</summary>
    /// <typeparam name="T">An enum type.</typeparam>
    public T ReadEnum<T>() where T : unmanaged, Enum
    {
        switch (Type.GetTypeCode(typeof(T)))
        {
            case TypeCode.Int32:
            {
                var value = ReadInt32();
                return Unsafe.As<int, T>(ref value);
            }
            case TypeCode.UInt32:
            {
                var value = ReadUInt32();
                return Unsafe.As<uint, T>(ref value);
            }
            case TypeCode.Int64:
            {
                var value = ReadInt64();
                return Unsafe.As<long, T>(ref value);
            }
            case TypeCode.UInt64:
            {
                var value = ReadUInt64();
                return Unsafe.As<ulong, T>(ref value);
            }
            case TypeCode.Int16:
            {
                var value = ReadInt16();
                return Unsafe.As<short, T>(ref value);
            }
            case TypeCode.UInt16:
            {
                var value = ReadUInt16();
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
