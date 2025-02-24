using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Backdash.Core;
using Backdash.Network;

namespace Backdash.Serialization;

/// <summary>
/// Binary span reader.
/// </summary>
[DebuggerDisplay("Read: {ReadCount}")]
public readonly ref struct BinaryBufferReader
{
    /// <summary>
    /// Initialize a new <see cref="BinaryBufferReader"/> for <paramref name="buffer"/>
    /// </summary>
    /// <param name="buffer">Byte buffer to be read</param>
    /// <param name="offset">Read offset reference</param>
    /// <param name="endianness">Deserialization endianness</param>
    public BinaryBufferReader(ReadOnlySpan<byte> buffer, ref int offset, Endianness endianness = Endianness.BigEndian)
    {
        this.offset = ref offset;
        this.buffer = buffer;
        Endianness = endianness;
    }

    readonly ref int offset;
    readonly ReadOnlySpan<byte> buffer;

    /// <summary>Gets or init the value to define which endianness should be used for serialization.</summary>
    public readonly Endianness Endianness;

    /// <summary>Total read byte count.</summary>
    public int ReadCount => offset;

    /// <summary>Total buffer capacity in bytes.</summary>
    public int Capacity => buffer.Length;

    /// <summary>Available buffer space in bytes</summary>
    public int FreeCapacity => Capacity - ReadCount;

    /// <summary>
    /// Return full buffer
    /// </summary>
    public ReadOnlySpan<byte> Buffer => buffer;

    /// <summary>Returns a <see cref="Span{Byte}"/> for the current available buffer.</summary>
    public ReadOnlySpan<byte> CurrentBuffer => buffer[offset..];

    /// <summary>Advance read pointer by <paramref name="count"/>.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count) => offset += count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Span<T> GetListSpan<T>(in List<T> values)
    {
        var count = ReadInt32();
        CollectionsMarshal.SetCount(values, count);
        var span = CollectionsMarshal.AsSpan(values);
        return span;
    }

    void ReadSpan<T>(in Span<T> data) where T : struct => ReadByte(MemoryMarshal.AsBytes(data));

    /// <summary>Reads single <see cref="byte"/> from buffer.</summary>
    public byte ReadByte() => buffer[offset++];

    /// <summary>Reads a span of <see cref="byte"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadByte(in Span<byte> values)
    {
        var length = values.Length;
        if (length > FreeCapacity) length = FreeCapacity;

        var slice = buffer.Slice(offset, length);
        Advance(length);
        slice.CopyTo(values[..length]);
    }

    /// <summary>Reads a list of <see cref="byte"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadByte(in List<byte> values) => ReadByte(GetListSpan(in values));

    /// <summary>Reads single <see cref="sbyte"/> from buffer.</summary>
    public sbyte ReadSByte() => unchecked((sbyte)buffer[offset++]);

    /// <summary>Reads a span of <see cref="sbyte"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadSByte(in Span<sbyte> values) => ReadSpan(values);

    /// <summary>Reads a list of <see cref="sbyte"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadSByte(in List<sbyte> values) => ReadSByte(GetListSpan(in values));

    /// <summary>Reads single <see cref="bool"/> from buffer.</summary>
    public bool ReadBoolean()
    {
        var value = BitConverter.ToBoolean(CurrentBuffer);
        Advance(sizeof(bool));
        return value;
    }

    /// <summary>Reads a span of <see cref="bool"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadBoolean(in Span<bool> values) => ReadSpan(values);

    /// <summary>Reads a list of <see cref="bool"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadBoolean(in List<bool> values) => ReadBoolean(GetListSpan(in values));

    /// <summary>Reads single <see cref="short"/> from buffer.</summary>
    public short ReadInt16() => ReadNumber<short>(false);

    /// <summary>Reads a span of <see cref="short"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadInt16(in Span<short> values)
    {
        ReadSpan(values);
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(values, values);
    }

    /// <summary>Reads a list of <see cref="short"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadInt16(in List<short> values) => ReadInt16(GetListSpan(in values));

    /// <summary>Reads single <see cref="ushort"/> from buffer.</summary>
    public ushort ReadUInt16() => ReadNumber<ushort>(true);

    /// <summary>Reads a span of <see cref="ushort"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadUInt16(in Span<ushort> values)
    {
        ReadSpan(values);
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(values, values);
    }

    /// <summary>Reads a list of <see cref="ushort"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadUInt16(in List<ushort> values) => ReadUInt16(GetListSpan(in values));

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

    /// <summary>Reads a list of <see cref="char"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadChar(in List<char> values) => ReadChar(GetListSpan(in values));

    /// <summary>Reads single <see cref="char"/> from buffer.</summary>
    public char ReadUtf8Char()
    {
        Span<char> result = stackalloc char[1];
        ReadUtf8String(in result);
        return result[0];
    }

    /// <summary>Reads a span of UTF8 <see cref="char"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadUtf8String(in Span<char> values)
    {
        var byteCount = System.Text.Encoding.UTF8.GetByteCount(values);
        System.Text.Encoding.UTF8.GetChars(CurrentBuffer[..byteCount], values);
        Advance(byteCount);
    }

    /// <summary>Reads a list of UTF8 <see cref="char"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadUtf8String(in List<char> values) => ReadUtf8String(GetListSpan(in values));

    /// <summary>Reads single <see cref="int"/> from buffer.</summary>
    public int ReadInt32() => ReadNumber<int>(false);

    /// <summary>Reads a span of <see cref="int"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadInt32(in Span<int> values)
    {
        ReadSpan(values);
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(values, values);
    }

    /// <summary>Reads a list of <see cref="int"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadInt32(in List<int> values) => ReadInt32(GetListSpan(in values));

    /// <summary>Reads single <see cref="uint"/> from buffer.</summary>
    public uint ReadUInt32() => ReadNumber<uint>(true);

    /// <summary>Reads a span of <see cref="uint"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadUInt32(in Span<uint> values)
    {
        ReadSpan(values);
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(values, values);
    }

    /// <summary>Reads a list of <see cref="uint"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadUInt32(in List<uint> values) => ReadUInt32(GetListSpan(in values));

    /// <summary>Reads single <see cref="long"/> from buffer.</summary>
    public long ReadInt64() => ReadNumber<long>(false);

    /// <summary>Reads a span of <see cref="long"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadInt64(in Span<long> values)
    {
        ReadSpan(values);
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(values, values);
    }

    /// <summary>Reads a list of <see cref="long"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadInt64(in List<long> values) => ReadInt64(GetListSpan(in values));

    /// <summary>Reads single <see cref="ulong"/> from buffer.</summary>
    public ulong ReadUInt64() => ReadNumber<ulong>(true);

    /// <summary>Reads a span of <see cref="ulong"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadUInt64(in Span<ulong> values)
    {
        ReadSpan(values);
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(values, values);
    }

    /// <summary>Reads a list of <see cref="ulong"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadUInt64(in List<ulong> values) => ReadUInt64(GetListSpan(in values));

    /// <summary>Reads single <see cref="Int128"/> from buffer.</summary>
    public Int128 ReadInt128() => ReadNumber<Int128>(false);

    /// <summary>Reads a span of <see cref="Int128"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadInt128(in Span<Int128> values)
    {
        ReadSpan(values);
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(values, values);
    }

    /// <summary>Reads a list of <see cref="Int128"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadInt128(in List<Int128> values) => ReadInt128(GetListSpan(in values));

    /// <summary>Reads single <see cref="UInt128"/> from buffer.</summary>
    public UInt128 ReadUInt128() => ReadNumber<UInt128>(true);

    /// <summary>Reads a span of <see cref="UInt128"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadUInt128(in Span<UInt128> values)
    {
        ReadSpan(values);
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(values, values);
    }

    /// <summary>Reads a list of <see cref="UInt128"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadUInt128(in List<UInt128> values) => ReadUInt128(GetListSpan(in values));


    /// <summary>Reads single <see cref="Half"/> from buffer.</summary>
    public Half ReadHalf() => BitConverter.Int16BitsToHalf(ReadInt16());

    /// <summary>Reads span of Half 32 <see cref="Half"/> from buffer.</summary>
    public void ReadHalf(in Span<Half> values) => ReadInt16(MemoryMarshal.Cast<Half, short>(values));

    /// <summary>Reads a list of <see cref="Half"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadHalf(in List<Half> values) => ReadHalf(GetListSpan(in values));


    /// <inheritdoc cref="ReadFloat()"/>
    /// <seealso cref="ReadFloat()"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadSingle() => ReadFloat();

    /// <summary>Reads float 32 <see cref="float"/> from buffer.</summary>
    public float ReadFloat() => BitConverter.Int32BitsToSingle(ReadInt32());

    /// <summary>Reads span of float 32 <see cref="float"/> from buffer.</summary>
    public void ReadFloat(in Span<float> values) => ReadInt32(MemoryMarshal.Cast<float, int>(values));

    /// <summary>Reads a list of <see cref="float"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadFloat(in List<float> values) => ReadFloat(GetListSpan(in values));


    /// <summary>Reads single <see cref="double"/> from buffer.</summary>
    public double ReadDouble() => BitConverter.Int64BitsToDouble(ReadInt64());

    /// <summary>Reads span of double 32 <see cref="double"/> from buffer.</summary>
    public void ReadDouble(in Span<double> values) => ReadInt64(MemoryMarshal.Cast<double, long>(values));

    /// <summary>Reads a list of <see cref="double"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadDouble(in List<double> values) => ReadDouble(GetListSpan(in values));

    /// <summary>Reads single <see cref="Guid"/> from buffer.</summary>
    public Guid ReadGuid()
    {
        var span = CurrentBuffer[..Unsafe.SizeOf<Guid>()];
        var result = new Guid(span, Endianness is Endianness.BigEndian);
        Advance(span.Length);
        return result;
    }

    /// <summary>Reads a span of <see cref="Guid"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadGuid(in Span<Guid> values)
    {
        if (values.IsEmpty) return;
        ref var current = ref MemoryMarshal.GetReference(values);
        ref var limit = ref Unsafe.Add(ref current, values.Length);

        while (Unsafe.IsAddressLessThan(ref current, ref limit))
        {
            current = ReadGuid();
            current = ref Unsafe.Add(ref current, 1)!;
        }
    }

    /// <summary>Reads a list of <see cref="Guid"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadGuid(in List<Guid> values) => ReadGuid(GetListSpan(in values));


    /// <summary>Reads single <see cref="TimeSpan"/> from buffer.</summary>
    public TimeSpan ReadTimeSpan() => new(ReadInt64());

    /// <summary>Reads a span of <see cref="TimeSpan"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadTimeSpan(in Span<TimeSpan> values)
    {
        if (values.IsEmpty) return;
        ReadInt64(MemoryMarshal.Cast<TimeSpan, long>(values));
    }

    /// <summary>Reads a list of <see cref="TimeSpan"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadTimeSpan(in List<TimeSpan> values) => ReadTimeSpan(GetListSpan(in values));


    /// <summary>Reads single <see cref="TimeOnly"/> from buffer.</summary>
    public TimeOnly ReadTimeOnly() => new(ReadInt64());

    /// <summary>Reads a span of <see cref="TimeOnly"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadTimeOnly(in Span<TimeOnly> values)
    {
        if (values.IsEmpty) return;
        ReadInt64(MemoryMarshal.Cast<TimeOnly, long>(values));
    }

    /// <summary>Reads a list of <see cref="TimeOnly"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadTimeOnly(in List<TimeOnly> values) => ReadTimeOnly(GetListSpan(in values));

    /// <summary>Reads single <see cref="DateTime"/> from buffer.</summary>
    public DateTime ReadDateTime()
    {
        var kind = (DateTimeKind)ReadByte();
        return new(ReadInt64(), kind);
    }

    /// <summary>Reads a span of <see cref="DateTime"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadDateTime(in Span<DateTime> values)
    {
        if (values.IsEmpty) return;
        ref var current = ref MemoryMarshal.GetReference(values);
        ref var limit = ref Unsafe.Add(ref current, values.Length);

        while (Unsafe.IsAddressLessThan(ref current, ref limit))
        {
            current = ReadDateTime();
            current = ref Unsafe.Add(ref current, 1)!;
        }
    }

    /// <summary>Reads a list of <see cref="DateTime"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadDateTime(in List<DateTime> values) => ReadDateTime(GetListSpan(in values));

    /// <summary>Reads single <see cref="DateTimeOffset"/> from buffer.</summary>
    public DateTimeOffset ReadDateTimeOffset()
    {
        var dtOffset = ReadTimeSpan();
        return new(ReadInt64(), dtOffset);
    }

    /// <summary>Reads a span of <see cref="DateTimeOffset"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadDateTimeOffset(in Span<DateTimeOffset> values)
    {
        if (values.IsEmpty) return;
        ref var current = ref MemoryMarshal.GetReference(values);
        ref var limit = ref Unsafe.Add(ref current, values.Length);

        while (Unsafe.IsAddressLessThan(ref current, ref limit))
        {
            current = ReadDateTimeOffset();
            current = ref Unsafe.Add(ref current, 1)!;
        }
    }

    /// <summary>Reads a list of <see cref="DateTimeOffset"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadDateTimeOffset(in List<DateTimeOffset> values) => ReadDateTimeOffset(GetListSpan(in values));


    /// <summary>Reads single <see cref="DateOnly"/> from buffer.</summary>
    public DateOnly ReadDateOnly() => DateOnly.FromDayNumber(ReadInt32());

    /// <summary>Reads a span of <see cref="DateOnly"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadDateOnly(in Span<DateOnly> values)
    {
        if (values.IsEmpty) return;
        ReadInt32(MemoryMarshal.Cast<DateOnly, int>(values));
    }

    /// <summary>Reads a list of <see cref="DateOnly"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadDateOnly(in List<DateOnly> values) => ReadDateOnly(GetListSpan(in values));

    /// <summary>Reads an unmanaged struct from buffer.</summary>
    public void ReadStruct<T>(ref T value) where T : unmanaged
    {
        var size = Unsafe.SizeOf<T>();
        if (size > FreeCapacity) size = FreeCapacity;
        var bytes = Mem.AsBytes(ref value);
        CurrentBuffer[..size].CopyTo(bytes);
        Advance(size);
    }

    /// <summary>Reads an unmanaged struct from buffer.</summary>
    public T ReadStruct<T>() where T : unmanaged
    {
        var size = Unsafe.SizeOf<T>();
        if (size > FreeCapacity) size = FreeCapacity;
        var result = Mem.ReadStruct<T>(CurrentBuffer[..size]);
        Advance(size);
        return result;
    }

    /// <summary>Reads an unmanaged struct list from buffer.</summary>
    public void ReadStruct<T>(in List<T> values) where T : unmanaged =>
        ReadStruct(GetListSpan(in values));

    /// <summary>Reads an unmanaged struct span from buffer.</summary>
    public void ReadStruct<T>(in Span<T> values) where T : unmanaged =>
        ReadByte(MemoryMarshal.AsBytes(values));

    /// <summary>Reads an unmanaged struct from buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReadStruct<T>(in T[] values) where T : unmanaged =>
        ReadStruct(values.AsSpan());

    /// <summary>Reads an unmanaged struct from buffer.</summary>
    public T ReadStructUnsafe<T>() where T : struct
    {
        var size = Unsafe.SizeOf<T>();
        if (size > FreeCapacity) size = FreeCapacity;
        var result = Mem.ReadStruct<T>(CurrentBuffer[..size]);
        Advance(size);
        return result;
    }

    /// <summary>Reads an unmanaged struct from buffer.</summary>
    public void ReadStructUnsafe<T>(in Span<T> values) where T : struct
    {
        ThrowHelpers.ThrowIfTypeIsReferenceOrContainsReferences<T>();
        var valuesBytes = MemoryMarshal.AsBytes(values);
        ReadByte(in valuesBytes);
    }

    /// <summary>Reads and allocates an <see cref="string"/> from buffer.</summary>
    public string ReadString(int size)
    {
        Span<char> charBuffer = stackalloc char[size];
        ReadChar(in charBuffer);
        return new(charBuffer);
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

    /// <summary>Reads a <see cref="IBinarySerializable"/> <paramref name="value"/> from buffer.</summary>
    /// <typeparam name="T">A type that implements <see cref="IBinarySerializable"/>.</typeparam>
    public void Read<T>(ref T value) where T : IBinarySerializable => value.Deserialize(in this);

    /// <summary>Reads a span of <see cref="IBinarySerializable"/> <paramref name="values"/> into buffer.</summary>
    /// <typeparam name="T">A type that implements <see cref="IBinarySerializable"/>.</typeparam>
    public void Read<T>(in Span<T> values) where T : IBinarySerializable
    {
        if (values.IsEmpty) return;
        ref var current = ref MemoryMarshal.GetReference(values);
        ref var limit = ref Unsafe.Add(ref current, values.Length);

        while (Unsafe.IsAddressLessThan(ref current, ref limit))
        {
            current.Deserialize(in this);
            current = ref Unsafe.Add(ref current, 1)!;
        }
    }

    /// <summary>Reads an array of <see cref="IBinarySerializable"/> <paramref name="values"/> into buffer.</summary>
    /// <typeparam name="T">A type that implements <see cref="IBinarySerializable"/>.</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Read<T>(in T[] values) where T : IBinarySerializable => Read(values.AsSpan());

    /// <summary>Writes an array of <see cref="IBinarySerializable"/> <paramref name="values"/> into buffer.</summary>
    /// <typeparam name="T">A type that implements <see cref="IBinarySerializable"/>.</typeparam>
    public void Read<T>(in List<T> values) where T : IBinarySerializable => Read(GetListSpan(in values));
}
