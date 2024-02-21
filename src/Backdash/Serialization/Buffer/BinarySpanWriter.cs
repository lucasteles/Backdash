using System.Buffers.Binary;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Backdash.Network;

namespace Backdash.Serialization.Buffer;

public readonly ref struct BinarySpanWriter
{
    public BinarySpanWriter(scoped in Span<byte> buffer, ref int offset)
    {
        this.buffer = buffer;
        this.offset = ref offset;
    }

    readonly ref int offset;
    readonly Span<byte> buffer;
    public Endianness Endianness { get; init; } = Endianness.BigEndian;
    public int WrittenCount => offset;
    public int Capacity => buffer.Length;
    public int FreeCapacity => Capacity - WrittenCount;

    public Span<byte> CurrentBuffer => buffer[offset..];

    public void Advance(int count) => offset += count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteSpan<T>(in ReadOnlySpan<T> data) where T : struct => Write(MemoryMarshal.AsBytes(data));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Span<T> AllocSpanFor<T>(in ReadOnlySpan<T> value) where T : struct
    {
        var sizeBytes = Unsafe.SizeOf<T>() * value.Length;
        var result = MemoryMarshal.Cast<byte, T>(buffer.Slice(offset, sizeBytes));
        Advance(sizeBytes);
        return result;
    }

    public void Write(in byte value) => buffer[offset++] = value;

    public void Write(in sbyte value) => buffer[offset++] = unchecked((byte)value);

    public void Write(in bool value)
    {
        BitConverter.TryWriteBytes(CurrentBuffer, value).AssertTrue();
        Advance(sizeof(bool));
    }

    public void Write(in short value) => WriteNumber(in value);
    public void Write(in ushort value) => WriteNumber(in value);
    public void Write(in int value) => WriteNumber(in value);

    public void Write(in uint value) => WriteNumber(in value);
    public void Write(in char value) => Write((ushort)value);
    public void Write(in long value) => WriteNumber(in value);
    public void Write(in ulong value) => WriteNumber(in value);
    public void Write(in Int128 value) => WriteNumber(in value);
    public void Write(in UInt128 value) => WriteNumber(in value);
    public void Write(in Half value) => Write(BitConverter.HalfToInt16Bits(value));
    public void Write(in float value) => Write(BitConverter.SingleToInt32Bits(value));
    public void Write(in double value) => Write(BitConverter.DoubleToInt64Bits(value));

    public void Write(Vector2 value)
    {
        Write(value.X);
        Write(value.Y);
    }

    public void Write(Vector3 value)
    {
        Write(value.X);
        Write(value.Y);
        Write(value.Z);
    }

    public void Write(Vector4 value)
    {
        Write(value.X);
        Write(value.Y);
        Write(value.Z);
        Write(value.W);
    }

    public void Write(Quaternion value)
    {
        Write(value.X);
        Write(value.Y);
        Write(value.Z);
        Write(value.W);
    }

    public void Write(in ReadOnlySpan<byte> value)
    {
        if (value.Length > FreeCapacity)
            throw new InvalidOperationException("Not available buffer space");

        value.CopyTo(CurrentBuffer);
        Advance(value.Length);
    }

    public void Write(in ReadOnlySpan<sbyte> value) => WriteSpan(in value);
    public void Write(in ReadOnlySpan<bool> value) => WriteSpan(in value);

    public void Write(in ReadOnlySpan<short> value)
    {
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(value, AllocSpanFor(in value));
        else
            WriteSpan(in value);
    }

    public void Write(in ReadOnlySpan<ushort> value)
    {
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(value, AllocSpanFor(in value));
        else
            WriteSpan(in value);
    }

    public void Write(in ReadOnlySpan<char> value) => Write(MemoryMarshal.Cast<char, ushort>(value));

    public void Write(in ReadOnlySpan<int> value)
    {
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(value, AllocSpanFor(in value));
        else
            WriteSpan(in value);
    }

    public void Write(in ReadOnlySpan<uint> value)
    {
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(value, AllocSpanFor(in value));
        else
            WriteSpan(in value);
    }

    public void Write(in ReadOnlySpan<long> value)
    {
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(value, AllocSpanFor(in value));
        else
            WriteSpan(in value);
    }

    public void Write(in ReadOnlySpan<ulong> value)
    {
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(value, AllocSpanFor(in value));
        else
            WriteSpan(in value);
    }

    public void Write(in ReadOnlySpan<Int128> value)
    {
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(value, AllocSpanFor(in value));
        else
            WriteSpan(in value);
    }

    public void Write(in ReadOnlySpan<UInt128> value)
    {
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(value, AllocSpanFor(in value));
        else
            WriteSpan(in value);
    }

    public void WriteNumber<T>(in T value) where T : unmanaged, IBinaryInteger<T>
    {
        ref var valueRef = ref Unsafe.AsRef(in value);
        var size = Unsafe.SizeOf<T>();

        switch (Endianness)
        {
            case Endianness.LittleEndian:
                valueRef.WriteLittleEndian(CurrentBuffer[..size]);
                break;
            case Endianness.BigEndian:
                valueRef.WriteBigEndian(CurrentBuffer[..size]);
                break;
            default:
                return;
        }

        Advance(size);
    }

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
