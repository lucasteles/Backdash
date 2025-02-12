using System.Buffers;
using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Backdash.Core;
using Backdash.Network;

namespace Backdash.Serialization.Buffer;

/// <summary>
/// Binary span writer.
/// </summary>
/// <remarks>
/// Initialize a new <see cref="BinarySpanWriter"/> for <paramref name="buffer"/>
/// </remarks>
/// <param name="buffer">Byte buffer to be written</param>
public readonly struct BinaryBufferWriter(IBufferWriter<byte> buffer)
{
    /// <summary>
    /// Gets or init the value to define which endianness should be used for serialization.
    /// </summary>
    public Endianness Endianness { get; init; } = Endianness.BigEndian;

    /// <summary>
    /// Backing IBufferWriter <see cref="IBufferWriter{T}"/>
    /// </summary>
    public IBufferWriter<byte> Buffer => buffer;

    /// <summary>Advance write pointer by <paramref name="count"/>.</summary>
    public void Advance(int count) => buffer.Advance(count);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteSpan<T>(in ReadOnlySpan<T> data) where T : unmanaged => Write(MemoryMarshal.AsBytes(data));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Span<T> AllocSpan<T>(in ReadOnlySpan<T> value) where T : unmanaged
    {
        var sizeBytes = Unsafe.SizeOf<T>() * value.Length;
        var result = MemoryMarshal.Cast<byte, T>(buffer.GetSpan(sizeBytes));
        Advance(sizeBytes);
        return result;
    }

    /// <summary>Writes a span bytes of <see cref="byte"/> <paramref name="value"/> into buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(in ReadOnlySpan<byte> value) => buffer.Write(value);

    /// <summary>Writes single <see cref="byte"/> <paramref name="value"/> into buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(in byte value) => buffer.Write(Mem.AsSpan(in value));

    /// <summary>Writes single <see cref="sbyte"/> <paramref name="value"/> into buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Write(in sbyte value) => Write(unchecked((byte)value));

    /// <summary>Writes single <see cref="bool"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in bool value)
    {
        const int size = sizeof(bool);
        if (!BitConverter.TryWriteBytes(buffer.GetSpan(size), value))
            throw new NetcodeException("Destination too short");

        Advance(size);
    }

    /// <summary>Writes single <see cref="short"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in short value) => WriteNumber(in value);

    /// <summary>Writes single <see cref="ushort"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in ushort value) => WriteNumber(in value);

    /// <summary>Writes single <see cref="int"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in int value) => WriteNumber(in value);

    /// <summary>Writes single <see cref="uint"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in uint value) => WriteNumber(in value);

    /// <summary>Writes single <see cref="char"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in char value) => Write((ushort)value);

    /// <summary>Writes single <see cref="long"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in long value) => WriteNumber(in value);

    /// <summary>Writes single <see cref="ulong"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in ulong value) => WriteNumber(in value);

    /// <summary>Writes single <see cref="Int128"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in Int128 value) => WriteNumber(in value);

    /// <summary>Writes single <see cref="UInt128"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in UInt128 value) => WriteNumber(in value);

    /// <summary>Writes single <see cref="Half"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in Half value) => Write(BitConverter.HalfToInt16Bits(value));

    /// <summary>Writes single <see cref="float"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in float value) => Write(BitConverter.SingleToInt32Bits(value));

    /// <summary>Writes single <see cref="double"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in double value) => Write(BitConverter.DoubleToInt64Bits(value));

    /// <summary>Writes single <see cref="Vector2"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in Vector2 value)
    {
        Write(value.X);
        Write(value.Y);
    }

    /// <summary>Writes single <see cref="Vector3"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in Vector3 value)
    {
        Write(value.X);
        Write(value.Y);
        Write(value.Z);
    }

    /// <summary>Writes single <see cref="Vector4"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in Vector4 value)
    {
        Write(value.X);
        Write(value.Y);
        Write(value.Z);
        Write(value.W);
    }

    /// <summary>Writes single <see cref="Quaternion"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in Quaternion value)
    {
        Write(value.X);
        Write(value.Y);
        Write(value.Z);
        Write(value.W);
    }

    /// <summary>Writes a span of <see cref="sbyte"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in ReadOnlySpan<sbyte> value) => WriteSpan(in value);

    /// <summary>Writes a span of <see cref="bool"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in ReadOnlySpan<bool> value) => WriteSpan(in value);

    /// <summary>Writes a span of <see cref="short"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in ReadOnlySpan<short> value)
    {
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(value, AllocSpan(in value));
        else
            WriteSpan(in value);
    }

    /// <summary>Writes a span of <see cref="ushort"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in ReadOnlySpan<ushort> value)
    {
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(value, AllocSpan(in value));
        else
            WriteSpan(in value);
    }

    /// <summary>Writes a span of <see cref="char"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in ReadOnlySpan<char> value) => Write(MemoryMarshal.Cast<char, ushort>(value));

    /// <summary>Writes a span of <see cref="int"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in ReadOnlySpan<int> value)
    {
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(value, AllocSpan(in value));
        else
            WriteSpan(in value);
    }

    /// <summary>Writes a span of <see cref="uint"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in ReadOnlySpan<uint> value)
    {
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(value, AllocSpan(in value));
        else
            WriteSpan(in value);
    }

    /// <summary>Writes a span of <see cref="long"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in ReadOnlySpan<long> value)
    {
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(value, AllocSpan(in value));
        else
            WriteSpan(in value);
    }

    /// <summary>Writes a span of <see cref="ulong"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in ReadOnlySpan<ulong> value)
    {
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(value, AllocSpan(in value));
        else
            WriteSpan(in value);
    }

    /// <summary>Writes a span of <see cref="Int128"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in ReadOnlySpan<Int128> value)
    {
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(value, AllocSpan(in value));
        else
            WriteSpan(in value);
    }

    /// <summary>Writes a span of <see cref="UInt128"/> <paramref name="value"/> into buffer.</summary>
    public void Write(in ReadOnlySpan<UInt128> value)
    {
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(value, AllocSpan(in value));
        else
            WriteSpan(in value);
    }

    /// <summary>Writes an unmanaged struct into buffer.</summary>
    public void WriteStruct<T>(in T value) where T : unmanaged => Write(Mem.AsBytes(in value));

    /// <summary>Writes an unmanaged struct span into buffer.</summary>
    public void WriteStruct<T>(ReadOnlySpan<T> values) where T : unmanaged => Write(MemoryMarshal.AsBytes(values));

    /// <summary>Writes an unmanaged struct span into buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteStruct<T>(in T[] values) where T : unmanaged => WriteStruct<T>(values.AsSpan());

    /// <summary>Writes a struct into buffer.</summary>
    public void WriteStructUnsafe<T>(in T value) where T : struct => Write(Mem.AsBytesUnsafe(in value));

    /// <summary>Writes a struct span into buffer.</summary>
    public void WriteStructUnsafe<T>(ReadOnlySpan<T> values) where T : struct
    {
        ThrowHelpers.ThrowIfTypeIsReferenceOrContainsReferences<T>();
        Write(MemoryMarshal.AsBytes(values));
    }

    /// <summary>Writes an <see cref="string"/> <paramref name="value"/> into buffer.</summary>
    public void WriteString(in string value) => Write(value.AsSpan());

    /// <summary>Writes an <see cref="string"/> <paramref name="value"/> into buffer as UTF8.</summary>
    public void WriteUtf8String(in ReadOnlySpan<char> value)
    {
        var span = buffer.GetSpan(System.Text.Encoding.UTF8.GetByteCount(value));
        var writtenCount = System.Text.Encoding.UTF8.GetBytes(value, span);
        Advance(writtenCount);
    }

    /// <summary>Writes an <see cref="char"/> <paramref name="value"/> into buffer as UTF8.</summary>
    public void WriteUtf8Char(in char value) => WriteUtf8String(Mem.AsSpan(in value));

    /// <summary>Writes a <see cref="IBinaryInteger{T}"/> <paramref name="value"/> into buffer.</summary>
    /// <typeparam name="T">A numeric type that implements <see cref="IBinaryInteger{T}"/>.</typeparam>
    public void WriteNumber<T>(in T value) where T : unmanaged, IBinaryInteger<T>
    {
        ref var valueRef = ref Unsafe.AsRef(in value);
        var size = Unsafe.SizeOf<T>();
        switch (Endianness)
        {
            case Endianness.LittleEndian:
                valueRef.WriteLittleEndian(buffer.GetSpan(size));
                break;
            case Endianness.BigEndian:
                valueRef.WriteBigEndian(buffer.GetSpan(size));
                break;
            default:
                return;
        }

        Advance(size);
    }

    /// <summary>Writes the <see cref="Enum"/> <paramref name="enumValue"/> into buffer.</summary>
    /// <typeparam name="T">An enum type.</typeparam>
    public void WriteEnum<T>(in T enumValue) where T : unmanaged, Enum
    {
        var refValue = Unsafe.AsRef(in enumValue);
        switch (Type.GetTypeCode(typeof(T)))
        {
            case TypeCode.Int32:
            {
                var tmp = Unsafe.As<T, int>(ref refValue);
                Write(in tmp);
                break;
            }
            case TypeCode.UInt32:
            {
                var tmp = Unsafe.As<T, uint>(ref refValue);
                Write(in tmp);
                break;
            }
            case TypeCode.Int64:
            {
                var tmp = Unsafe.As<T, long>(ref refValue);
                Write(in tmp);
                break;
            }
            case TypeCode.UInt64:
            {
                var tmp = Unsafe.As<T, ulong>(ref refValue);
                Write(in tmp);
                break;
            }
            case TypeCode.Int16:
            {
                var tmp = Unsafe.As<T, short>(ref refValue);
                Write(in tmp);
                break;
            }
            case TypeCode.UInt16:
            {
                var tmp = Unsafe.As<T, ushort>(ref refValue);
                Write(in tmp);
                break;
            }
            case TypeCode.Byte:
            {
                var tmp = Unsafe.As<T, byte>(ref refValue);
                Write(in tmp);
                break;
            }
            case TypeCode.SByte:
            {
                var tmp = Unsafe.As<T, sbyte>(ref refValue);
                Write(in tmp);
                break;
            }
            default: throw new InvalidOperationException("Unknown enum underlying type");
        }
    }
}
