using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Backdash.Core;
using Backdash.Data;
using Backdash.Network;

namespace Backdash.Serialization;

/// <summary>
///     Binary span reader.
/// </summary>
[DebuggerDisplay("Read: {ReadCount}")]
public readonly ref struct BinaryBufferReader
{
    /// <summary>
    ///     Initialize a new <see cref="BinaryBufferReader" /> for <paramref name="buffer" />
    /// </summary>
    /// <param name="buffer">Byte buffer to be read</param>
    /// <param name="offset">Read offset reference</param>
    /// <param name="endianness">Deserialization endianness</param>
    public BinaryBufferReader(ReadOnlySpan<byte> buffer, ref int offset, Endianness? endianness = null)
    {
        this.offset = ref offset;
        this.buffer = buffer;
        Endianness = endianness ?? Platform.Endianness;
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
    ///     Return full buffer
    /// </summary>
    public ReadOnlySpan<byte> Buffer => buffer;

    /// <summary>Returns a <see cref="Span{Byte}" /> for the current available buffer.</summary>
    public ReadOnlySpan<byte> CurrentBuffer => buffer[offset..];

    /// <summary>Advance read pointer by <paramref name="count" />.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Advance(int count) => offset += count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Span<T> GetListSpan<T>(in List<T> values) where T : struct
    {
        var count = ReadInt32();
        CollectionsMarshal.SetCount(values, count);
        return CollectionsMarshal.AsSpan(values);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Span<T> GetListSpan<T>(in List<T> values, in IObjectPool<T> pool) where T : class
    {
        var count = ReadInt32();

        for (var i = count; i < values.Count; i++)
            pool.Return(values[i]);

        CollectionsMarshal.SetCount(values, count);
        return CollectionsMarshal.AsSpan(values);
    }

    /// <summary>
    ///     Advance and allocates a Span of size <paramref name="size" /> for type <typeparamref name="T" />> in the buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<T> AllocSpan<T>(int size) where T : struct
    {
        var span = CurrentBuffer[..(size * Unsafe.SizeOf<T>())];
        Advance(size);
        return MemoryMarshal.Cast<byte, T>(span);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void ReadSpan<T>(in Span<T> data) where T : struct => Read(MemoryMarshal.AsBytes(data));

    /// <summary>Reads single <see cref="byte" /> from buffer.</summary>
    public byte ReadByte() => buffer[offset++];

    /// <inheritdoc cref="ReadByte()" />
    public byte? ReadNullableByte() => ReadBoolean() ? ReadByte() : null;

    /// <summary>Reads single <see cref="sbyte" /> from buffer.</summary>
    public sbyte ReadSByte() => unchecked((sbyte)buffer[offset++]);

    /// <inheritdoc cref="ReadSByte()" />
    public sbyte? ReadNullableSByte() => ReadBoolean() ? ReadSByte() : null;

    /// <summary>Reads single <see cref="bool" /> from buffer.</summary>
    public bool ReadBoolean()
    {
        var value = BitConverter.ToBoolean(CurrentBuffer);
        Advance(sizeof(bool));
        return value;
    }

    /// <inheritdoc cref="ReadBoolean()" />
    public bool? ReadNullableBoolean() => ReadBoolean() ? ReadBoolean() : null;

    /// <summary>Reads single <see cref="short" /> from buffer.</summary>
    public short ReadInt16() => ReadNumber<short>(false);

    /// <inheritdoc cref="ReadInt16()" />
    public short? ReadNullableInt16() => ReadBoolean() ? ReadInt16() : null;

    /// <summary>Reads single <see cref="ushort" /> from buffer.</summary>
    public ushort ReadUInt16() => ReadNumber<ushort>(true);

    /// <inheritdoc cref="ReadUInt16()" />
    public ushort? ReadNullableUInt16() => ReadBoolean() ? ReadUInt16() : null;

    /// <summary>Reads single <see cref="char" /> from buffer.</summary>
    public char ReadChar() => (char)ReadUInt16();

    /// <inheritdoc cref="ReadChar()" />
    public char? ReadNullableChar() => ReadBoolean() ? ReadChar() : null;

    /// <summary>Reads single <see cref="int" /> from buffer.</summary>
    public int ReadInt32() => ReadNumber<int>(false);

    /// <inheritdoc cref="ReadInt32()" />
    public int? ReadNullableInt32() => ReadBoolean() ? ReadInt32() : null;

    /// <summary>Reads single <see cref="uint" /> from buffer.</summary>
    public uint ReadUInt32() => ReadNumber<uint>(true);

    /// <inheritdoc cref="ReadUInt32()" />
    public uint? ReadNullableUInt32() => ReadBoolean() ? ReadUInt32() : null;

    /// <summary>Reads single <see cref="long" /> from buffer.</summary>
    public long ReadInt64() => ReadNumber<long>(false);

    /// <inheritdoc cref="ReadInt64()" />
    public long? ReadNullableInt64() => ReadBoolean() ? ReadInt64() : null;

    /// <summary>Reads single <see cref="ulong" /> from buffer.</summary>
    public ulong ReadUInt64() => ReadNumber<ulong>(true);

    /// <inheritdoc cref="ReadUInt64()" />
    public ulong? ReadNullableUInt64() => ReadBoolean() ? ReadUInt64() : null;

    /// <summary>Reads single <see cref="Int128" /> from buffer.</summary>
    public Int128 ReadInt128() => ReadNumber<Int128>(false);

    /// <inheritdoc cref="ReadInt128()" />
    public Int128? ReadNullableInt128() => ReadBoolean() ? ReadInt128() : null;

    /// <summary>Reads single <see cref="UInt128" /> from buffer.</summary>
    public UInt128 ReadUInt128() => ReadNumber<UInt128>(true);

    /// <inheritdoc cref="ReadUInt128()" />
    public UInt128? ReadNullableUInt128() => ReadBoolean() ? ReadUInt128() : null;

    /// <summary>Reads single <see cref="Half" /> from buffer.</summary>
    public Half ReadHalf() => BitConverter.Int16BitsToHalf(ReadInt16());

    /// <inheritdoc cref="ReadHalf()" />
    public Half? ReadNullableHalf() => ReadBoolean() ? ReadHalf() : null;

    /// <inheritdoc cref="ReadFloat()" />
    /// <seealso cref="ReadFloat()" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float ReadSingle() => ReadFloat();

    /// <inheritdoc cref="ReadNullableFloat()" />
    /// <seealso cref="ReadNullableFloat()" />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float? ReadNullableSingle() => ReadNullableFloat();

    /// <summary>Reads float 32 <see cref="float" /> from buffer.</summary>
    public float ReadFloat() => BitConverter.Int32BitsToSingle(ReadInt32());

    /// <inheritdoc cref="ReadFloat()" />
    public float? ReadNullableFloat() => ReadBoolean() ? ReadFloat() : null;

    /// <summary>Reads single <see cref="double" /> from buffer.</summary>
    public double ReadDouble() => BitConverter.Int64BitsToDouble(ReadInt64());

    /// <inheritdoc cref="ReadDouble()" />
    public double? ReadNullableDouble() => ReadBoolean() ? ReadDouble() : null;

    /// <summary>Reads single <see cref="Guid" /> from buffer.</summary>
    public Guid ReadGuid()
    {
        var span = CurrentBuffer[..Unsafe.SizeOf<Guid>()];
        var result = new Guid(span, Endianness is Endianness.BigEndian);
        Advance(span.Length);
        return result;
    }

    /// <inheritdoc cref="ReadGuid()" />
    public Guid? ReadNullableGuid() => ReadBoolean() ? ReadGuid() : null;

    /// <summary>Reads single <see cref="TimeSpan" /> from buffer.</summary>
    public TimeSpan ReadTimeSpan() => new(ReadInt64());

    /// <inheritdoc cref="ReadTimeSpan()" />
    public TimeSpan? ReadNullableTimeSpan() => ReadBoolean() ? ReadTimeSpan() : null;

    /// <summary>Reads single <see cref="TimeOnly" /> from buffer.</summary>
    public TimeOnly ReadTimeOnly() => new(ReadInt64());

    /// <inheritdoc cref="ReadTimeOnly()" />
    public TimeOnly? ReadNullableTimeOnly() => ReadBoolean() ? ReadTimeOnly() : null;

    /// <summary>Reads single <see cref="DateTime" /> from buffer.</summary>
    public DateTime ReadDateTime()
    {
        var kind = (DateTimeKind)ReadByte();
        return new(ReadInt64(), kind);
    }

    /// <inheritdoc cref="ReadDateTime()" />
    public DateTime? ReadNullableDateTime() => ReadBoolean() ? ReadDateTime() : null;

    /// <summary>Reads single <see cref="DateTimeOffset" /> from buffer.</summary>
    public DateTimeOffset ReadDateTimeOffset()
    {
        var dtOffset = ReadTimeSpan();
        return new(ReadInt64(), dtOffset);
    }

    /// <inheritdoc cref="ReadDateTimeOffset()" />
    public DateTimeOffset? ReadNullableDateTimeOffset() => ReadBoolean() ? ReadDateTimeOffset() : null;

    /// <summary>Reads single <see cref="DateOnly" /> from buffer.</summary>
    public DateOnly ReadDateOnly() => DateOnly.FromDayNumber(ReadInt32());

    /// <inheritdoc cref="ReadDateOnly()" />
    public DateOnly? ReadNullableDateOnly() => ReadBoolean() ? ReadDateOnly() : null;

    /// <summary>Reads single <see cref="Frame" /> from buffer.</summary>
    public Frame ReadFrame() => ReadAsInt32<Frame>();

    /// <inheritdoc cref="ReadFrame()" />
    public Frame? ReadNullableFrame() => ReadBoolean() ? ReadFrame() : null;

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
    public void ReadStruct<T>(ref T? value) where T : unmanaged =>
        value = ReadNullableStruct<T>();

    /// <summary>Reads an unmanaged struct from buffer.</summary>
    public T ReadStruct<T>() where T : unmanaged
    {
        var size = Unsafe.SizeOf<T>();
        var result = MemoryMarshal.Read<T>(CurrentBuffer[..size]);
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

    /// <inheritdoc cref="ReadStruct{T}()" />
    public T? ReadNullableStruct<T>() where T : unmanaged =>
        ReadBoolean() ? ReadStruct<T>() : null;

    /// <summary>Reads and allocates an <see cref="string" /> from buffer.</summary>
    public string ReadString(int size)
    {
        Span<char> charBuffer = stackalloc char[size];
        Read(in charBuffer);
        return new(charBuffer);
    }

    /// <summary>Reads a span of UTF8 <see cref="char" /> from buffer into <paramref name="values" />.</summary>
    public void ReadUtf8String(in Span<char> values)
    {
        var byteCount = Encoding.UTF8.GetByteCount(values);
        Encoding.UTF8.GetChars(CurrentBuffer[..byteCount], values);
        Advance(byteCount);
    }

    /// <summary>Reads a list of UTF8 <see cref="char" /> from buffer into <paramref name="values" />.</summary>
    public void ReadUtf8String(in List<char> values) => ReadUtf8String(GetListSpan(in values));

    /// <summary>Reads single <see cref="IBinaryInteger{T}" /> from buffer.</summary>
    /// <typeparam name="T">A numeric type that implements <see cref="IBinaryInteger{T}" />.</typeparam>
    /// <param name="isUnsigned">
    ///     true if source represents an unsigned two's complement number; otherwise, false to indicate it
    ///     represents a signed two's complement number
    /// </param>
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

    /// <summary>Reads single <see cref="IBinaryInteger{T}" /> from buffer.</summary>
    /// <typeparam name="T">A numeric type that implements <see cref="IBinaryInteger{T}" /> and <see cref="IMinMaxValue{T}" />.</typeparam>
    public T ReadNumber<T>() where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T> =>
        ReadNumber<T>(T.IsZero(T.MinValue));

    /// <inheritdoc cref="ReadNumber{T}()" />
    public void ReadNumber<T>(ref T value) where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T> =>
        value = ReadNumber<T>();

    /// <inheritdoc cref="ReadNullableNumber{T}()" />
    public void ReadNumber<T>(ref T? value) where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T> =>
        value = ReadNullableNumber<T>();

    /// <inheritdoc cref="ReadNumber{T}(bool)" />
    public void ReadNumber<T>(ref T value, bool isUnsigned) where T : unmanaged, IBinaryInteger<T> =>
        value = ReadNumber<T>(isUnsigned);

    /// <inheritdoc cref="ReadNullableNumber{T}(bool)" />
    public void ReadNumber<T>(ref T? value, bool isUnsigned) where T : unmanaged, IBinaryInteger<T> =>
        value = ReadNullableNumber<T>(isUnsigned);

    /// <inheritdoc cref="ReadNumber{T}()" />
    public T? ReadNullableNumber<T>() where T : unmanaged, IBinaryInteger<T>, IMinMaxValue<T> =>
        ReadBoolean() ? ReadNumber<T>() : null;

    /// <inheritdoc cref="ReadNumber{T}(bool)" />
    public T? ReadNullableNumber<T>(bool isUnsigned) where T : unmanaged, IBinaryInteger<T> =>
        ReadBoolean() ? ReadNumber<T>(isUnsigned) : null;

    /// <summary>Reads a <see cref="IBinarySerializable" /> <typeparamref name="T" /> from buffer.</summary>
    /// <typeparam name="T">A value type that implements <see cref="IBinarySerializable" />.</typeparam>
    public T? ReadNullable<T>() where T : struct, IBinarySerializable
    {
        T? value = new();
        Read(ref value);
        return value;
    }

    /// <summary>Reads a nullable <see cref="IBinarySerializable" /> <paramref name="value" /> from buffer.</summary>
    /// <typeparam name="T">A nullable reference type that implements <see cref="IBinarySerializable" />.</typeparam>
    public void ReadNullable<T>(ref T? value, IObjectPool<T> pool, bool forceReturn = true)
        where T : class, IBinarySerializable
    {
        if (ReadBoolean())
        {
            if (forceReturn)
            {
                if (value is not null)
                    pool.Return(value);

                value = pool.Rent();
            }
            else
                value ??= pool.Rent();

            Read(value);
        }
        else
            value = null;
    }

    /// <summary>Reads a nullable <see cref="IBinarySerializable" /> <paramref name="value" /> from buffer.</summary>
    /// <typeparam name="T">A nullable reference type that implements <see cref="IBinarySerializable" />.</typeparam>
    public void ReadNullable<T>(ref T? value, bool forceReturn = true) where T : class, IBinarySerializable, new() =>
        ReadNullable(ref value, DefaultObjectPool<T>.Instance, forceReturn);

    /// <summary>Reads a <see cref="IBinarySerializable" /> <paramref name="value" /> from buffer.</summary>
    /// <typeparam name="T">A value type that implements <see cref="IBinarySerializable" />.</typeparam>
    public void Read<T>(ref T value) where T : struct, IBinarySerializable => value.Deserialize(in this);

    /// <summary>Reads a <see cref="IBinarySerializable" /> <typeparamref name="T" /> from buffer.</summary>
    /// <typeparam name="T">A value type that implements <see cref="IBinarySerializable" />.</typeparam>
    public T Read<T>() where T : struct, IBinarySerializable
    {
        T value = new();
        value.Deserialize(in this);
        return value;
    }

    /// <summary>Reads a <see cref="IBinarySerializable" /> <paramref name="value" /> from buffer.</summary>
    /// <typeparam name="T">A value type that implements <see cref="IBinarySerializable" />.</typeparam>
    public void Read<T>(ref T? value) where T : struct, IBinarySerializable =>
        value = ReadBoolean() ? Read<T>() : null;

    /// <summary>Reads a span of <see cref="IBinarySerializable" /> <paramref name="values" /> into buffer.</summary>
    /// <typeparam name="T">A list of a value type that implements <see cref="IBinarySerializable" />.</typeparam>
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

    /// <summary>Reads an array of <see cref="IBinarySerializable" /> <paramref name="values" /> from buffer.</summary>
    /// <typeparam name="T">A type that implements <see cref="IBinarySerializable" />.</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Read<T>(in T[] values) where T : struct, IBinarySerializable => Read(values.AsSpan());

    /// <summary>Reads an array of unmanaged <see cref="IBinarySerializable" /> <paramref name="values" /> from buffer.</summary>
    /// <typeparam name="T">A value type that implements <see cref="IBinarySerializable" />.</typeparam>
    public void Read<T>(in List<T> values) where T : struct, IBinarySerializable => Read(GetListSpan(in values));

    /// <summary>
    ///     Reads a circular buffer of unmanaged <see cref="IBinarySerializable" /> <paramref name="values" /> from
    ///     buffer.
    /// </summary>
    /// <typeparam name="T">A value type that implements <see cref="IBinarySerializable" />.</typeparam>
    public void Read<T>(in CircularBuffer<T> values) where T : IBinarySerializable
    {
        var size = ReadInt32();
        var span = values.GetResetSpan(size);
        Read(in span);
    }

    /// <summary>Reads a <see cref="IBinarySerializable" /> <paramref name="value" /> from buffer.</summary>
    /// <typeparam name="T">A reference type that implements <see cref="IBinarySerializable" />.</typeparam>
    public void Read<T>(T value) where T : class, IBinarySerializable => value.Deserialize(in this);

    /// <inheritdoc cref="Read{T}(T)" />
    public void Read<T>(
        ref T? value, bool nullable, bool forceReturn = true
    ) where T : class, IBinarySerializable, new()
    {
        if (nullable)
            ReadNullable(ref value, forceReturn);
        else
            Read(value!);
    }

    /// <summary>Reads a span of <see cref="IBinarySerializable" /> <paramref name="values" /> into buffer.</summary>
    /// <typeparam name="T">A list of a reference type that implements <see cref="IBinarySerializable" />.</typeparam>
    public void Read<T>(in Span<T> values, in IObjectPool<T> pool) where T : class, IBinarySerializable
    {
        if (values.IsEmpty) return;
        ref var current = ref MemoryMarshal.GetReference(values);
        ref var limit = ref Unsafe.Add(ref current, values.Length);

        while (Unsafe.IsAddressLessThan(ref current, ref limit))
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            // ReSharper disable once ConvertIfStatementToNullCoalescingAssignment
#pragma warning disable IDE0074
            if (current is null)
#pragma warning restore IDE0074
                current = pool.Rent();
            current.Deserialize(in this);
            current = ref Unsafe.Add(ref current, 1)!;
        }
    }

    /// <summary>Reads an array of <see cref="IBinarySerializable" /> <paramref name="values" /> into buffer.</summary>
    /// <typeparam name="T">A reference type that implements <see cref="IBinarySerializable" />.</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Read<T>(in T[] values, in IObjectPool<T> pool) where T : class, IBinarySerializable =>
        Read(values.AsSpan(), in pool);

    /// <summary>Reads an array of <see cref="IBinarySerializable" /> <paramref name="values" /> into buffer.</summary>
    /// <typeparam name="T">A reference that implements <see cref="IBinarySerializable" />.</typeparam>
    public void Read<T>(in List<T> values, IObjectPool<T> pool) where T : class, IBinarySerializable =>
        Read(GetListSpan(in values, pool), in pool);

    /// <summary>
    ///     Reads a span of <see cref="IBinarySerializable" /> <paramref name="values" /> from buffer.
    /// </summary>
    /// <seealso cref="Read{T}(in Span{T},in IObjectPool{T})" />
    /// <typeparam name="T">A reference that implements <see cref="IBinarySerializable" />.</typeparam>
    public void Read<T>(Span<T> values) where T : class, IBinarySerializable, new() =>
        Read(values, in DefaultObjectPool<T>.Instance);

    /// <summary>
    ///     Reads an array of <see cref="IBinarySerializable" /> <paramref name="values" /> from buffer.
    /// </summary>
    /// <seealso cref="Read{T}(in T[],in IObjectPool{T})" />
    /// <typeparam name="T">A reference that implements <see cref="IBinarySerializable" />.</typeparam>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Read<T>(T[] values) where T : class, IBinarySerializable, new() =>
        Read(values.AsSpan());

    /// <summary>
    ///     Reads a list of <see cref="IBinarySerializable" /> <paramref name="values" /> from buffer.
    /// </summary>
    /// <seealso cref="Read{T}(in List{T}, IObjectPool{T})" />
    /// <typeparam name="T">A reference that implements <see cref="IBinarySerializable" />.</typeparam>
    public void Read<T>(List<T> values) where T : class, IBinarySerializable, new() =>
        Read(GetListSpan(in values, in DefaultObjectPool<T>.Instance));

    /// <summary>
    ///     Reads a StringBuilder into <paramref name="values" /> from buffer.
    /// </summary>
    public void Read(in StringBuilder values)
    {
        var size = ReadInt32();
        var chars = AllocSpan<char>(size);
        values.Clear();
        values.Append(chars);
    }

    /// <inheritdoc cref="ReadByte()" />
    public void Read(ref byte value) => value = ReadByte();

    /// <inheritdoc cref="ReadByte()" />
    public void Read(ref byte? value) => value = ReadNullableByte();

    /// <inheritdoc cref="ReadSByte()" />
    public void Read(ref sbyte value) => value = ReadSByte();

    /// <inheritdoc cref="ReadSByte()" />
    public void Read(ref sbyte? value) => value = ReadNullableSByte();

    /// <inheritdoc cref="ReadBoolean()" />
    public void Read(ref bool value) => value = ReadBoolean();

    /// <inheritdoc cref="ReadBoolean()" />
    public void Read(ref bool? value) => value = ReadNullableBoolean();

    /// <inheritdoc cref="ReadInt16()" />
    public void Read(ref short value) => value = ReadInt16();

    /// <inheritdoc cref="ReadInt16()" />
    public void Read(ref short? value) => value = ReadNullableInt16();

    /// <inheritdoc cref="ReadInt16()" />
    public void Read(ref ushort value) => value = ReadUInt16();

    /// <inheritdoc cref="ReadInt16()" />
    public void Read(ref ushort? value) => value = ReadNullableUInt16();

    /// <inheritdoc cref="ReadChar()" />
    public void Read(ref char value) => value = ReadChar();

    /// <inheritdoc cref="ReadChar()" />
    public void Read(ref char? value) => value = ReadNullableChar();

    /// <inheritdoc cref="ReadInt32()" />
    public void Read(ref int value) => value = ReadInt32();

    /// <inheritdoc cref="ReadInt32()" />
    public void Read(ref int? value) => value = ReadNullableInt32();

    /// <inheritdoc cref="ReadUInt32()" />
    public void Read(ref uint value) => value = ReadUInt32();

    /// <inheritdoc cref="ReadUInt32()" />
    public void Read(ref uint? value) => value = ReadNullableUInt32();

    /// <inheritdoc cref="ReadInt64()" />
    public void Read(ref long value) => value = ReadInt64();

    /// <inheritdoc cref="ReadInt64()" />
    public void Read(ref long? value) => value = ReadNullableInt64();

    /// <inheritdoc cref="ReadUInt64()" />
    public void Read(ref ulong value) => value = ReadUInt64();

    /// <inheritdoc cref="ReadUInt64()" />
    public void Read(ref ulong? value) => value = ReadNullableUInt64();

    /// <inheritdoc cref="ReadInt128()" />
    public void Read(ref Int128 value) => value = ReadInt128();

    /// <inheritdoc cref="ReadInt128()" />
    public void Read(ref Int128? value) => value = ReadNullableInt128();

    /// <inheritdoc cref="ReadUInt128()" />
    public void Read(ref UInt128 value) => value = ReadUInt128();

    /// <inheritdoc cref="ReadUInt128()" />
    public void Read(ref UInt128? value) => value = ReadNullableUInt128();

    /// <inheritdoc cref="ReadHalf()" />
    public void Read(ref Half value) => value = ReadHalf();

    /// <inheritdoc cref="ReadHalf()" />
    public void Read(ref Half? value) => value = ReadNullableHalf();

    /// <inheritdoc cref="ReadFloat()" />
    public void Read(ref float value) => value = ReadFloat();

    /// <inheritdoc cref="ReadFloat()" />
    public void Read(ref float? value) => value = ReadNullableFloat();

    /// <inheritdoc cref="ReadDouble()" />
    public void Read(ref double value) => value = ReadDouble();

    /// <inheritdoc cref="ReadDouble()" />
    public void Read(ref double? value) => value = ReadNullableDouble();

    /// <inheritdoc cref="ReadGuid()" />
    public void Read(ref Guid value) => value = ReadGuid();

    /// <inheritdoc cref="ReadDouble()" />
    public void Read(ref Guid? value) => value = ReadNullableGuid();

    /// <inheritdoc cref="ReadTimeSpan()" />
    public void Read(ref TimeSpan value) => value = ReadTimeSpan();

    /// <inheritdoc cref="ReadTimeSpan()" />
    public void Read(ref TimeSpan? value) => value = ReadNullableTimeSpan();

    /// <inheritdoc cref="ReadDateTime()" />
    public void Read(ref DateTime value) => value = ReadDateTime();

    /// <inheritdoc cref="ReadDateTime()" />
    public void Read(ref DateTime? value) => value = ReadNullableDateTime();

    /// <inheritdoc cref="ReadDateTimeOffset()" />
    public void Read(ref DateTimeOffset value) => value = ReadDateTimeOffset();

    /// <inheritdoc cref="ReadDateTimeOffset()" />
    public void Read(ref DateTimeOffset? value) => value = ReadNullableDateTimeOffset();

    /// <inheritdoc cref="ReadTimeOnly()" />
    public void Read(ref TimeOnly value) => value = ReadTimeOnly();

    /// <inheritdoc cref="ReadTimeOnly()" />
    public void Read(ref TimeOnly? value) => value = ReadNullableTimeOnly();

    /// <inheritdoc cref="ReadDateOnly()" />
    public void Read(ref DateOnly value) => value = ReadDateOnly();

    /// <inheritdoc cref="ReadTimeOnly()" />
    public void Read(ref DateOnly? value) => value = ReadNullableDateOnly();

    /// <inheritdoc cref="ReadFrame()" />
    public void Read(ref Frame value) => value = ReadFrame();

    /// <inheritdoc cref="ReadTimeOnly()" />
    public void Read(ref Frame? value) => value = ReadNullableFrame();

    /// <summary>Reads a span of <see cref="byte" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in Span<byte> values)
    {
        var length = values.Length;
        if (length > FreeCapacity) length = FreeCapacity;

        var slice = buffer.Slice(offset, length);
        Advance(length);
        slice.CopyTo(values[..length]);
    }

    /// <summary>Reads a list of <see cref="byte" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in List<byte> values) => Read(GetListSpan(in values));

    /// <summary>Reads a span of <see cref="sbyte" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in Span<sbyte> values) => ReadSpan(values);

    /// <summary>Reads a list of <see cref="sbyte" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in List<sbyte> values) => Read(GetListSpan(in values));

    /// <summary>Reads a span of <see cref="bool" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in Span<bool> values) => ReadSpan(values);

    /// <summary>Reads a list of <see cref="bool" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in List<bool> values) => Read(GetListSpan(in values));

    /// <summary>Reads a span of <see cref="short" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in Span<short> values)
    {
        ReadSpan(values);
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(values, values);
    }

    /// <summary>Reads a list of <see cref="short" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in List<short> values) => Read(GetListSpan(in values));

    /// <summary>Reads a span of <see cref="ushort" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in Span<ushort> values)
    {
        ReadSpan(values);
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(values, values);
    }

    /// <summary>Reads a list of <see cref="ushort" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in List<ushort> values) => Read(GetListSpan(in values));

    /// <summary>Reads a span of <see cref="char" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in Span<char> values)
    {
        ReadSpan(values);
        if (Endianness != Platform.Endianness)
        {
            var ushortSpan = MemoryMarshal.Cast<char, ushort>(values);
            BinaryPrimitives.ReverseEndianness(ushortSpan, ushortSpan);
        }
    }

    /// <summary>Reads a list of <see cref="char" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in List<char> values) => Read(GetListSpan(in values));

    /// <summary>Reads a span of <see cref="int" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in Span<int> values)
    {
        ReadSpan(values);
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(values, values);
    }

    /// <summary>Reads a list of <see cref="int" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in List<int> values) => Read(GetListSpan(in values));

    /// <summary>Reads a span of <see cref="uint" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in Span<uint> values)
    {
        ReadSpan(values);
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(values, values);
    }

    /// <summary>Reads a list of <see cref="uint" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in List<uint> values) => Read(GetListSpan(in values));

    /// <summary>Reads a span of <see cref="long" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in Span<long> values)
    {
        ReadSpan(values);
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(values, values);
    }

    /// <summary>Reads a list of <see cref="long" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in List<long> values) => Read(GetListSpan(in values));

    /// <summary>Reads a span of <see cref="ulong" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in Span<ulong> values)
    {
        ReadSpan(values);
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(values, values);
    }

    /// <summary>Reads a list of <see cref="ulong" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in List<ulong> values) => Read(GetListSpan(in values));

    /// <summary>Reads a span of <see cref="Int128" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in Span<Int128> values)
    {
        ReadSpan(values);
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(values, values);
    }

    /// <summary>Reads a list of <see cref="Int128" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in List<Int128> values) => Read(GetListSpan(in values));

    /// <summary>Reads a span of <see cref="UInt128" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in Span<UInt128> values)
    {
        ReadSpan(values);
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(values, values);
    }

    /// <summary>Reads a list of <see cref="UInt128" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in List<UInt128> values) => Read(GetListSpan(in values));

    /// <summary>Reads span of Half 32 <see cref="Half" /> from buffer.</summary>
    public void Read(in Span<Half> values) => Read(MemoryMarshal.Cast<Half, short>(values));

    /// <summary>Reads a list of <see cref="Half" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in List<Half> values) => Read(GetListSpan(in values));

    /// <summary>Reads span of float 32 <see cref="float" /> from buffer.</summary>
    public void Read(in Span<float> values) => Read(MemoryMarshal.Cast<float, int>(values));

    /// <summary>Reads a list of <see cref="float" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in List<float> values) => Read(GetListSpan(in values));

    /// <summary>Reads span of double 32 <see cref="double" /> from buffer.</summary>
    public void Read(in Span<double> values) => Read(MemoryMarshal.Cast<double, long>(values));

    /// <summary>Reads a list of <see cref="double" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in List<double> values) => Read(GetListSpan(in values));

    /// <summary>Reads a span of <see cref="Guid" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in Span<Guid> values)
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

    /// <summary>Reads a list of <see cref="Guid" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in List<Guid> values) => Read(GetListSpan(in values));

    /// <summary>Reads a span of <see cref="TimeSpan" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in Span<TimeSpan> values) => Read(MemoryMarshal.Cast<TimeSpan, long>(values));

    /// <summary>Reads a list of <see cref="TimeSpan" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in List<TimeSpan> values) => Read(GetListSpan(in values));

    /// <summary>Reads a span of <see cref="TimeOnly" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in Span<TimeOnly> values) => Read(MemoryMarshal.Cast<TimeOnly, long>(values));

    /// <summary>Reads a list of <see cref="TimeOnly" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in List<TimeOnly> values) => Read(GetListSpan(in values));

    /// <summary>Reads a span of <see cref="DateTime" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in Span<DateTime> values)
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

    /// <summary>Reads a list of <see cref="DateTime" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in List<DateTime> values) => Read(GetListSpan(in values));

    /// <summary>Reads a span of <see cref="DateTimeOffset" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in Span<DateTimeOffset> values)
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

    /// <summary>Reads a list of <see cref="DateTimeOffset" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in List<DateTimeOffset> values) => Read(GetListSpan(in values));

    /// <summary>Reads a span of <see cref="DateOnly" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in Span<DateOnly> values)
    {
        if (values.IsEmpty) return;
        Read(MemoryMarshal.Cast<DateOnly, int>(values));
    }

    /// <summary>Reads a list of <see cref="DateOnly" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in List<DateOnly> values) => Read(GetListSpan(in values));

    /// <summary>Reads a span of <see cref="Frame" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in Span<Frame> values)
    {
        if (values.IsEmpty) return;
        Read(MemoryMarshal.Cast<Frame, int>(values));
    }

    /// <summary>Reads a list of <see cref="Frame" /> from buffer into <paramref name="values" />.</summary>
    public void Read(in List<Frame> values) => Read(GetListSpan(in values));


    /// <summary>Reads a <see cref="byte" /> from buffer and reinterprets it as <typeparamref name="T" />.</summary>
    public T ReadAsByte<T>() where T : unmanaged
    {
        var value = ReadByte();
        return Unsafe.As<byte, T>(ref value);
    }

    /// <inheritdoc cref="ReadAsByte{T}()" />
    public void ReadAsByte<T>(ref T value) where T : unmanaged => Read(ref Unsafe.As<T, byte>(ref value));

    /// <inheritdoc cref="ReadAsByte{T}()" />
    public void ReadAsByte<T>(ref T? value) where T : unmanaged => Read(ref Unsafe.As<T?, byte?>(ref value));

    /// <inheritdoc cref="ReadAsByte{T}()" />
    public void ReadAsByte<T>(in Span<T> values) where T : unmanaged => Read(MemoryMarshal.Cast<T, byte>(values));

    /// <inheritdoc cref="ReadAsByte{T}()" />
    public void ReadAsByte<T>(in List<T> values) where T : unmanaged => ReadAsByte(GetListSpan(in values));

    /// <inheritdoc cref="ReadAsByte{T}()" />
    public T? ReadAsNullableByte<T>() where T : unmanaged
    {
        var value = ReadNullableByte();
        return Unsafe.As<byte?, T?>(ref value);
    }

    /// <summary>Reads a <see cref="sbyte" /> from buffer and reinterprets it as <typeparamref name="T" />.</summary>
    public T ReadAsSByte<T>() where T : unmanaged
    {
        var value = ReadSByte();
        return Unsafe.As<sbyte, T>(ref value);
    }

    /// <inheritdoc cref="ReadAsSByte{T}()" />
    public void ReadAsSByte<T>(ref T value) where T : unmanaged => Read(ref Unsafe.As<T, sbyte>(ref value));

    /// <inheritdoc cref="ReadAsSByte{T}()" />
    public void ReadAsSByte<T>(ref T? value) where T : unmanaged => Read(ref Unsafe.As<T?, sbyte?>(ref value));

    /// <inheritdoc cref="ReadAsSByte{T}()" />
    public void ReadAsSByte<T>(in Span<T> values) where T : unmanaged => Read(MemoryMarshal.Cast<T, sbyte>(values));

    /// <inheritdoc cref="ReadAsSByte{T}()" />
    public void ReadAsSByte<T>(in List<T> values) where T : unmanaged => ReadAsSByte(GetListSpan(in values));

    /// <inheritdoc cref="ReadAsSByte{T}()" />
    public T? ReadAsNullableSByte<T>() where T : unmanaged
    {
        var value = ReadNullableSByte();
        return Unsafe.As<sbyte?, T?>(ref value);
    }


    /// <summary>Reads a <see cref="short" /> from buffer and reinterprets it as <typeparamref name="T" />.</summary>
    public T ReadAsInt16<T>() where T : unmanaged
    {
        var value = ReadInt16();
        return Unsafe.As<short, T>(ref value);
    }

    /// <inheritdoc cref="ReadAsInt16{T}()" />
    public void ReadAsInt16<T>(ref T value) where T : unmanaged => Read(ref Unsafe.As<T, short>(ref value));

    /// <inheritdoc cref="ReadAsInt16{T}()" />
    public void ReadAsInt16<T>(ref T? value) where T : unmanaged => Read(ref Unsafe.As<T?, short?>(ref value));

    /// <inheritdoc cref="ReadAsInt16{T}()" />
    public void ReadAsInt16<T>(in Span<T> values) where T : unmanaged => Read(MemoryMarshal.Cast<T, short>(values));

    /// <inheritdoc cref="ReadAsInt16{T}()" />
    public void ReadAsInt16<T>(in List<T> values) where T : unmanaged => ReadAsInt16(GetListSpan(in values));

    /// <inheritdoc cref="ReadAsInt16{T}()" />
    public T? ReadAsNullableInt16<T>() where T : unmanaged
    {
        var value = ReadNullableInt16();
        return Unsafe.As<short?, T?>(ref value);
    }

    /// <summary>Reads a <see cref="ushort" /> from buffer and reinterprets it as <typeparamref name="T" />.</summary>
    public T ReadAsUInt16<T>() where T : unmanaged
    {
        var value = ReadUInt16();
        return Unsafe.As<ushort, T>(ref value);
    }

    /// <inheritdoc cref="ReadAsUInt16{T}()" />
    public void ReadAsUInt16<T>(ref T value) where T : unmanaged => Read(ref Unsafe.As<T, ushort>(ref value));

    /// <inheritdoc cref="ReadAsUInt16{T}()" />
    public void ReadAsUInt16<T>(ref T? value) where T : unmanaged => Read(ref Unsafe.As<T?, ushort?>(ref value));

    /// <inheritdoc cref="ReadAsUInt16{T}()" />
    public void ReadAsUInt16<T>(in Span<T> values) where T : unmanaged => Read(MemoryMarshal.Cast<T, ushort>(values));

    /// <inheritdoc cref="ReadAsUInt16{T}()" />
    public void ReadAsUInt16<T>(in List<T> values) where T : unmanaged => ReadAsUInt16(GetListSpan(in values));

    /// <inheritdoc cref="ReadAsUInt16{T}()" />
    public T? ReadAsNullableUInt16<T>() where T : unmanaged
    {
        var value = ReadNullableUInt16();
        return Unsafe.As<ushort?, T?>(ref value);
    }

    /// <summary>Reads a <see cref="int" /> from buffer and reinterprets it as <typeparamref name="T" />.</summary>
    public T ReadAsInt32<T>() where T : unmanaged
    {
        var value = ReadInt32();
        return Unsafe.As<int, T>(ref value);
    }

    /// <inheritdoc cref="ReadAsInt32{T}()" />
    public void ReadAsInt32<T>(ref T value) where T : unmanaged => Read(ref Unsafe.As<T, int>(ref value));

    /// <inheritdoc cref="ReadAsInt32{T}()" />
    public void ReadAsInt32<T>(ref T? value) where T : unmanaged => Read(ref Unsafe.As<T?, int?>(ref value));

    /// <inheritdoc cref="ReadAsInt32{T}()" />
    public void ReadAsInt32<T>(in Span<T> values) where T : unmanaged => Read(MemoryMarshal.Cast<T, int>(values));

    /// <inheritdoc cref="ReadAsInt32{T}()" />
    public void ReadAsInt32<T>(in List<T> values) where T : unmanaged => ReadAsInt32(GetListSpan(in values));

    /// <inheritdoc cref="ReadAsInt32{T}()" />
    public T? ReadAsNullableInt32<T>() where T : unmanaged
    {
        var value = ReadNullableInt32();
        return Unsafe.As<int?, T?>(ref value);
    }

    /// <summary>Reads a <see cref="uint" /> from buffer and reinterprets it as <typeparamref name="T" />.</summary>
    public T ReadAsUInt32<T>() where T : unmanaged
    {
        var value = ReadUInt32();
        return Unsafe.As<uint, T>(ref value);
    }

    /// <inheritdoc cref="ReadAsUInt32{T}()" />
    public void ReadAsUInt32<T>(ref T value) where T : unmanaged => Read(ref Unsafe.As<T, uint>(ref value));

    /// <inheritdoc cref="ReadAsUInt32{T}()" />
    public void ReadAsUInt32<T>(ref T? value) where T : unmanaged => Read(ref Unsafe.As<T?, uint?>(ref value));

    /// <inheritdoc cref="ReadAsUInt32{T}()" />
    public void ReadAsUInt32<T>(in Span<T> values) where T : unmanaged => Read(MemoryMarshal.Cast<T, uint>(values));

    /// <inheritdoc cref="ReadAsUInt32{T}()" />
    public void ReadAsUInt32<T>(in List<T> values) where T : unmanaged => ReadAsUInt32(GetListSpan(in values));

    /// <inheritdoc cref="ReadAsUInt32{T}()" />
    public T? ReadAsNullableUInt32<T>() where T : unmanaged
    {
        var value = ReadNullableUInt32();
        return Unsafe.As<uint?, T?>(ref value);
    }

    /// <summary>Reads a <see cref="long" /> from buffer and reinterpret it as <typeparamref name="T" />.</summary>
    public T ReadAsInt64<T>() where T : unmanaged
    {
        var value = ReadInt64();
        return Unsafe.As<long, T>(ref value);
    }

    /// <inheritdoc cref="ReadAsInt64{T}()" />
    public void ReadAsInt64<T>(ref T value) where T : unmanaged => Read(ref Unsafe.As<T, long>(ref value));

    /// <inheritdoc cref="ReadAsInt64{T}()" />
    public void ReadAsInt64<T>(ref T? value) where T : unmanaged => Read(ref Unsafe.As<T?, long?>(ref value));

    /// <inheritdoc cref="ReadAsInt64{T}()" />
    public void ReadAsInt64<T>(in Span<T> values) where T : unmanaged => Read(MemoryMarshal.Cast<T, long>(values));

    /// <inheritdoc cref="ReadAsInt64{T}()" />
    public void ReadAsInt64<T>(in List<T> values) where T : unmanaged => ReadAsInt64(GetListSpan(in values));

    /// <inheritdoc cref="ReadAsInt64{T}()" />
    public T? ReadAsNullableInt64<T>() where T : unmanaged
    {
        var value = ReadNullableInt64();
        return Unsafe.As<long?, T?>(ref value);
    }

    /// <summary>Reads a <see cref="ulong" /> from buffer and reinterprets it as <typeparamref name="T" />.</summary>
    public T ReadAsUInt64<T>() where T : unmanaged
    {
        var value = ReadUInt64();
        return Unsafe.As<ulong, T>(ref value);
    }

    /// <inheritdoc cref="ReadAsUInt64{T}()" />
    public void ReadAsUInt64<T>(ref T value) where T : unmanaged => Read(ref Unsafe.As<T, ulong>(ref value));

    /// <inheritdoc cref="ReadAsUInt64{T}()" />
    public void ReadAsUInt64<T>(ref T? value) where T : unmanaged => Read(ref Unsafe.As<T?, ulong?>(ref value));

    /// <inheritdoc cref="ReadAsUInt64{T}()" />
    public void ReadAsUInt64<T>(in Span<T> values) where T : unmanaged => Read(MemoryMarshal.Cast<T, ulong>(values));

    /// <inheritdoc cref="ReadAsUInt64{T}()" />
    public void ReadAsUInt64<T>(in List<T> values) where T : unmanaged => ReadAsUInt64(GetListSpan(in values));

    /// <inheritdoc cref="ReadAsUInt64{T}()" />
    public T? ReadAsNullableUInt64<T>() where T : unmanaged
    {
        var value = ReadNullableUInt64();
        return Unsafe.As<ulong?, T?>(ref value);
    }
}
