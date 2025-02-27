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

    void ReadSpan<T>(in Span<T> data) where T : struct => Read(MemoryMarshal.AsBytes(data));

    /// <summary>Reads single <see cref="byte"/> from buffer.</summary>
    public byte ReadByte() => buffer[offset++];

    /// <inheritdoc cref="ReadByte()"/>
    public byte? ReadNullableByte() => ReadBoolean() ? ReadByte() : null;

    /// <summary>Reads single <see cref="sbyte"/> from buffer.</summary>
    public sbyte ReadSByte() => unchecked((sbyte)buffer[offset++]);

    /// <summary>Reads a span of <see cref="byte"/> from buffer into <paramref name="values"/>.</summary>
    public void Read(in Span<byte> values)
    {
        var length = values.Length;
        if (length > FreeCapacity) length = FreeCapacity;

        var slice = buffer.Slice(offset, length);
        Advance(length);
        slice.CopyTo(values[..length]);
    }

    /// <summary>Reads a list of <see cref="byte"/> from buffer into <paramref name="values"/>.</summary>
    public void Read(in List<byte> values) => Read(GetListSpan(in values));
    
    /// <summary>Reads a span of <see cref="sbyte"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadSByte(in Span<sbyte> values) => ReadSpan(values);

    /// <summary>Reads a list of <see cref="sbyte"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadSByte(in List<sbyte> values) => ReadSByte(GetListSpan(in values));

    /// <inheritdoc cref="ReadSByte()"/>
    public sbyte? ReadNullableSByte() => ReadBoolean() ? ReadSByte() : null;

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


    /// <inheritdoc cref="ReadBoolean()"/>
    public bool? ReadNullableBoolean() => ReadBoolean() ? ReadBoolean() : null;

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

    /// <inheritdoc cref="ReadInt16()"/>
    public short? ReadNullableInt16() => ReadBoolean() ? ReadInt16() : null;

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

    /// <inheritdoc cref="ReadUInt16()"/>
    public ushort? ReadNullableUInt16() => ReadBoolean() ? ReadUInt16() : null;

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

    /// <inheritdoc cref="ReadChar()"/>
    public char? ReadNullableChar() => ReadBoolean() ? ReadChar() : null;

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

    /// <inheritdoc cref="ReadInt32()"/>
    public int? ReadNullableInt32() => ReadBoolean() ? ReadInt32() : null;

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

    /// <inheritdoc cref="ReadUInt32()"/>
    public uint? ReadNullableUInt32() => ReadBoolean() ? ReadUInt32() : null;

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

    /// <inheritdoc cref="ReadInt64()"/>
    public long? ReadNullableInt64() => ReadBoolean() ? ReadInt64() : null;

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

    /// <inheritdoc cref="ReadUInt64()"/>
    public ulong? ReadNullableUInt64() => ReadBoolean() ? ReadUInt64() : null;

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

    /// <inheritdoc cref="ReadInt128()"/>
    public Int128? ReadNullableInt128() => ReadBoolean() ? ReadInt128() : null;

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

    /// <inheritdoc cref="ReadUInt128()"/>
    public UInt128? ReadNullableUInt128() => ReadBoolean() ? ReadUInt128() : null;

    /// <summary>Reads single <see cref="Half"/> from buffer.</summary>
    public Half ReadHalf() => BitConverter.Int16BitsToHalf(ReadInt16());

    /// <summary>Reads span of Half 32 <see cref="Half"/> from buffer.</summary>
    public void ReadHalf(in Span<Half> values) => ReadInt16(MemoryMarshal.Cast<Half, short>(values));

    /// <summary>Reads a list of <see cref="Half"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadHalf(in List<Half> values) => ReadHalf(GetListSpan(in values));

    /// <inheritdoc cref="ReadHalf()"/>
    public Half? ReadNullableHalf() => ReadBoolean() ? ReadHalf() : null;

    /// <inheritdoc cref="ReadFloat()"/>
    /// <seealso cref="ReadFloat()"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadSingle() => ReadFloat();

    /// <inheritdoc cref="ReadNullableFloat()"/>
    /// <seealso cref="ReadNullableFloat()"/>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float? ReadNullableSingle() => ReadNullableFloat();

    /// <summary>Reads float 32 <see cref="float"/> from buffer.</summary>
    public float ReadFloat() => BitConverter.Int32BitsToSingle(ReadInt32());

    /// <summary>Reads span of float 32 <see cref="float"/> from buffer.</summary>
    public void ReadFloat(in Span<float> values) => ReadInt32(MemoryMarshal.Cast<float, int>(values));

    /// <summary>Reads a list of <see cref="float"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadFloat(in List<float> values) => ReadFloat(GetListSpan(in values));

    /// <inheritdoc cref="ReadFloat()"/>
    public float? ReadNullableFloat() => ReadBoolean() ? ReadFloat() : null;

    /// <summary>Reads single <see cref="double"/> from buffer.</summary>
    public double ReadDouble() => BitConverter.Int64BitsToDouble(ReadInt64());

    /// <summary>Reads span of double 32 <see cref="double"/> from buffer.</summary>
    public void ReadDouble(in Span<double> values) => ReadInt64(MemoryMarshal.Cast<double, long>(values));

    /// <summary>Reads a list of <see cref="double"/> from buffer into <paramref name="values"/>.</summary>
    public void ReadDouble(in List<double> values) => ReadDouble(GetListSpan(in values));

    /// <inheritdoc cref="ReadDouble()"/>
    public double? ReadNullableDouble() => ReadBoolean() ? ReadDouble() : null;

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

    /// <inheritdoc cref="ReadGuid()"/>
    public Guid? ReadNullableGuid() => ReadBoolean() ? ReadGuid() : null;

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

    /// <inheritdoc cref="ReadTimeSpan()"/>
    public TimeSpan? ReadNullableTimeSpan() => ReadBoolean() ? ReadTimeSpan() : null;

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

    /// <inheritdoc cref="ReadTimeOnly()"/>
    public TimeOnly? ReadNullableTimeOnly() => ReadBoolean() ? ReadTimeOnly() : null;

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

    /// <inheritdoc cref="ReadDateTime()"/>
    public DateTime? ReadNullableDateTime() => ReadBoolean() ? ReadDateTime() : null;

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

    /// <inheritdoc cref="ReadDateTimeOffset()"/>
    public DateTimeOffset? ReadNullableDateTimeOffset() => ReadBoolean() ? ReadDateTimeOffset() : null;

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

    /// <inheritdoc cref="ReadDateOnly()"/>
    public DateOnly? ReadNullableDateOnly() => ReadBoolean() ? ReadDateOnly() : null;

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

    /// <summary>Reads an unmanaged struct span from buffer.</summary>
    public void ReadStruct<T>(in Span<T> values) where T : unmanaged =>
        Read(MemoryMarshal.AsBytes(values));

    /// <summary>Reads an unmanaged struct list from buffer.</summary>
    public void ReadStruct<T>(in List<T> values) where T : unmanaged =>
        ReadStruct(GetListSpan(in values));

    /// <summary>Reads an unmanaged struct from buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReadStruct<T>(in T[] values) where T : unmanaged =>
        ReadStruct(values.AsSpan());

    /// <inheritdoc cref="ReadStruct{T}()"/>
    public T? ReadNullableStruct<T>() where T : unmanaged =>
        ReadBoolean() ? ReadStruct<T>() : null;

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

    /// <inheritdoc cref="ReadNumber{T}()"/>
    public void ReadNumber<T>(ref T value) where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T> =>
        value = ReadNumber<T>();

    /// <inheritdoc cref="ReadNullableNumber{T}()"/>
    public void ReadNumber<T>(ref T? value) where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T> =>
        value = ReadNullableNumber<T>();

    /// <inheritdoc cref="ReadNumber{T}(bool)"/>
    public void ReadNumber<T>(ref T value, bool isUnsigned) where T : unmanaged, IBinaryInteger<T> =>
        value = ReadNumber<T>(isUnsigned);

    /// <inheritdoc cref="ReadNullableNumber{T}(bool)"/>
    public void ReadNumber<T>(ref T? value, bool isUnsigned) where T : unmanaged, IBinaryInteger<T> =>
        value = ReadNullableNumber<T>(isUnsigned);

    /// <inheritdoc cref="ReadNumber{T}()"/>
    public T? ReadNullableNumber<T>() where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T> =>
        ReadBoolean() ? ReadNumber<T>() : null;

    /// <inheritdoc cref="ReadNumber{T}(bool)"/>
    public T? ReadNullableNumber<T>(bool isUnsigned) where T : unmanaged, IBinaryInteger<T> =>
        ReadBoolean() ? ReadNumber<T>(isUnsigned) : null;

    /// <summary>Reads a <see cref="IBinarySerializable"/> <paramref name="value"/> from buffer.</summary>
    /// <typeparam name="T">A value type that implements <see cref="IBinarySerializable"/>.</typeparam>
    public void Read<T>(ref T value) where T : struct, IBinarySerializable => value.Deserialize(in this);

    /// <summary>Reads a <see cref="IBinarySerializable"/> <paramref name="value"/> from buffer.</summary>
    /// <typeparam name="T">A reference value type that implements <see cref="IBinarySerializable"/>.</typeparam>
    public void Read<T>(T value) where T : class, IBinarySerializable => value.Deserialize(in this);

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

    /// <inheritdoc cref="ReadByte()"/>
    public void Read(ref byte value) => value = ReadByte();

    /// <inheritdoc cref="ReadByte()"/>
    public void Read(ref byte? value) => value = ReadNullableByte();

    /// <inheritdoc cref="ReadSByte()"/>
    public void Read(ref sbyte value) => value = ReadSByte();

    /// <inheritdoc cref="ReadSByte()"/>
    public void Read(ref sbyte? value) => value = ReadNullableSByte();

    /// <inheritdoc cref="ReadBoolean()"/>
    public void Read(ref bool value) => value = ReadBoolean();

    /// <inheritdoc cref="ReadBoolean()"/>
    public void Read(ref bool? value) => value = ReadNullableBoolean();

    /// <inheritdoc cref="ReadInt16()"/>
    public void Read(ref short value) => value = ReadInt16();

    /// <inheritdoc cref="ReadInt16()"/>
    public void Read(ref short? value) => value = ReadNullableInt16();

    /// <inheritdoc cref="ReadInt16()"/>
    public void Read(ref ushort value) => value = ReadUInt16();

    /// <inheritdoc cref="ReadInt16()"/>
    public void Read(ref ushort? value) => value = ReadNullableUInt16();

    /// <inheritdoc cref="ReadChar()"/>
    public void Read(ref char value) => value = ReadChar();

    /// <inheritdoc cref="ReadChar()"/>
    public void Read(ref char? value) => value = ReadNullableChar();

    /// <inheritdoc cref="ReadInt32()"/>
    public void Read(ref int value) => value = ReadInt32();

    /// <inheritdoc cref="ReadInt32()"/>
    public void Read(ref int? value) => value = ReadNullableInt32();

    /// <inheritdoc cref="ReadUInt32()"/>
    public void Read(ref uint value) => value = ReadUInt32();

    /// <inheritdoc cref="ReadUInt32()"/>
    public void Read(ref uint? value) => value = ReadNullableUInt32();

    /// <inheritdoc cref="ReadInt64()"/>
    public void Read(ref long value) => value = ReadInt64();

    /// <inheritdoc cref="ReadInt64()"/>
    public void Read(ref long? value) => value = ReadNullableInt64();

    /// <inheritdoc cref="ReadUInt64()"/>
    public void Read(ref ulong value) => value = ReadUInt64();

    /// <inheritdoc cref="ReadUInt64()"/>
    public void Read(ref ulong? value) => value = ReadNullableUInt64();

    /// <inheritdoc cref="ReadInt128()"/>
    public void Read(ref Int128 value) => value = ReadInt128();

    /// <inheritdoc cref="ReadInt128()"/>
    public void Read(ref Int128? value) => value = ReadNullableInt128();

    /// <inheritdoc cref="ReadUInt128()"/>
    public void Read(ref UInt128 value) => value = ReadUInt128();

    /// <inheritdoc cref="ReadUInt128()"/>
    public void Read(ref UInt128? value) => value = ReadNullableUInt128();

    /// <inheritdoc cref="ReadHalf()"/>
    public void Read(ref Half value) => value = ReadHalf();

    /// <inheritdoc cref="ReadHalf()"/>
    public void Read(ref Half? value) => value = ReadNullableHalf();

    /// <inheritdoc cref="ReadFloat()"/>
    public void Read(ref float value) => value = ReadFloat();

    /// <inheritdoc cref="ReadFloat()"/>
    public void Read(ref float? value) => value = ReadNullableFloat();

    /// <inheritdoc cref="ReadDouble()"/>
    public void Read(ref double value) => value = ReadDouble();

    /// <inheritdoc cref="ReadDouble()"/>
    public void Read(ref double? value) => value = ReadNullableDouble();

    /// <inheritdoc cref="ReadGuid()"/>
    public void Read(ref Guid value) => value = ReadGuid();

    /// <inheritdoc cref="ReadDouble()"/>
    public void Read(ref Guid? value) => value = ReadNullableGuid();

    /// <inheritdoc cref="ReadTimeSpan()"/>
    public void Read(ref TimeSpan value) => value = ReadTimeSpan();

    /// <inheritdoc cref="ReadTimeSpan()"/>
    public void Read(ref TimeSpan? value) => value = ReadNullableTimeSpan();

    /// <inheritdoc cref="ReadDateTime()"/>
    public void Read(ref DateTime value) => value = ReadDateTime();

    /// <inheritdoc cref="ReadDateTime()"/>
    public void Read(ref DateTime? value) => value = ReadNullableDateTime();

    /// <inheritdoc cref="ReadDateTimeOffset()"/>
    public void Read(ref DateTimeOffset value) => value = ReadDateTimeOffset();

    /// <inheritdoc cref="ReadDateTimeOffset()"/>
    public void Read(ref DateTimeOffset? value) => value = ReadNullableDateTimeOffset();

    /// <inheritdoc cref="ReadTimeOnly()"/>
    public void Read(ref TimeOnly value) => value = ReadTimeOnly();

    /// <inheritdoc cref="ReadTimeOnly()"/>
    public void Read(ref TimeOnly? value) => value = ReadNullableTimeOnly();

    /// <inheritdoc cref="ReadDateOnly()"/>
    public void Read(ref DateOnly value) => value = ReadDateOnly();

    /// <inheritdoc cref="ReadTimeOnly()"/>
    public void Read(ref DateOnly? value) => value = ReadNullableDateOnly();
}

