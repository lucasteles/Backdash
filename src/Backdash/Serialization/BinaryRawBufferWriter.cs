using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Backdash.Core;
using Backdash.Data;
using Backdash.Network;

namespace Backdash.Serialization;

/// <summary>
///     Binary span writer.
/// </summary>
[DebuggerDisplay("Written: {WrittenCount}")]
public readonly ref struct BinaryRawBufferWriter
{
    /// <summary>
    ///     Initialize a new <see cref="BinaryRawBufferWriter" /> for <paramref name="buffer" />
    /// </summary>
    /// <param name="buffer">Byte buffer to be written</param>
    /// <param name="offset">Write offset reference</param>
    /// <param name="endianness">Serialization endianness</param>
    public BinaryRawBufferWriter(
        scoped in Span<byte> buffer,
        ref int offset,
        Endianness? endianness = null
    )
    {
        this.buffer = buffer;
        this.offset = ref offset;
        Endianness = endianness ?? Platform.Endianness;
    }

    readonly ref int offset;
    readonly Span<byte> buffer;

    /// <summary>Gets or init the value to define which endianness should be used for serialization.</summary>
    public readonly Endianness Endianness;

    /// <summary>Total written byte count.</summary>
    public int WrittenCount => offset;

    /// <summary>Total buffer capacity in bytes.</summary>
    public int Capacity => buffer.Length;

    /// <summary>Available buffer space in bytes</summary>
    public int FreeCapacity => Capacity - WrittenCount;

    /// <summary>Returns a <see cref="Span{Byte}" /> for the current available buffer.</summary>
    public Span<byte> CurrentBuffer => buffer[offset..];

    /// <summary>Advance write pointer by <paramref name="count" />.</summary>
    public void Advance(int count) => offset += count;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    void WriteSpan<T>(in ReadOnlySpan<T> data) where T : unmanaged => Write(MemoryMarshal.AsBytes(data));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Span<T> AllocSpan<T>(in ReadOnlySpan<T> value) where T : unmanaged
    {
        var sizeBytes = Unsafe.SizeOf<T>() * value.Length;
        var result = MemoryMarshal.Cast<byte, T>(buffer.Slice(offset, sizeBytes));
        Advance(sizeBytes);
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Span<T> GetListSpan<T>(in List<T> values)
    {
        var span = CollectionsMarshal.AsSpan(values);
        Write(span.Length);
        return span;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    Span<byte> GetSpan(int size)
    {
        var result = buffer.Slice(offset, size);
        Advance(size);
        return result;
    }

    /// <summary>Writes single <see cref="byte" /> <paramref name="value" /> into buffer.</summary>
    public void Write(in byte value) => buffer[offset++] = value;

    /// <summary>Writes single <see cref="sbyte" /> <paramref name="value" /> into buffer.</summary>
    public void Write(in sbyte value) => buffer[offset++] = unchecked((byte)value);

    /// <summary>Writes single <see cref="bool" /> <paramref name="value" /> into buffer.</summary>
    public void Write(in bool value)
    {
        if (!BitConverter.TryWriteBytes(CurrentBuffer, value))
            throw new NetcodeException("Destination is too short");
        Advance(sizeof(bool));
    }

    /// <summary>Writes single <see cref="short" /> <paramref name="value" /> into buffer.</summary>
    public void Write(in short value) => WriteNumber(in value);

    /// <summary>Writes single <see cref="ushort" /> <paramref name="value" /> into buffer.</summary>
    public void Write(in ushort value) => WriteNumber(in value);

    /// <summary>Writes single <see cref="int" /> <paramref name="value" /> into buffer.</summary>
    public void Write(in int value) => WriteNumber(in value);

    /// <summary>Writes single <see cref="uint" /> <paramref name="value" /> into buffer.</summary>
    public void Write(in uint value) => WriteNumber(in value);

    /// <summary>Writes single <see cref="char" /> <paramref name="value" /> into buffer.</summary>
    public void Write(in char value) => Write((ushort)value);

    /// <summary>Writes single <see cref="long" /> <paramref name="value" /> into buffer.</summary>
    public void Write(in long value) => WriteNumber(in value);

    /// <summary>Writes single <see cref="ulong" /> <paramref name="value" /> into buffer.</summary>
    public void Write(in ulong value) => WriteNumber(in value);

    /// <summary>Writes single <see cref="Int128" /> <paramref name="value" /> into buffer.</summary>
    public void Write(in Int128 value) => WriteNumber(in value);

    /// <summary>Writes single <see cref="UInt128" /> <paramref name="value" /> into buffer.</summary>
    public void Write(in UInt128 value) => WriteNumber(in value);

    /// <summary>Writes single <see cref="Half" /> <paramref name="value" /> into buffer.</summary>
    public void Write(in Half value) => Write(BitConverter.HalfToInt16Bits(value));

    /// <summary>Writes single <see cref="float" /> <paramref name="value" /> into buffer.</summary>
    public void Write(in float value) => Write(BitConverter.SingleToInt32Bits(value));

    /// <summary>Writes single <see cref="double" /> <paramref name="value" /> into buffer.</summary>
    public void Write(in double value) => Write(BitConverter.DoubleToInt64Bits(value));

    /// <summary>Writes single <see cref="Guid" /> <paramref name="value" /> into buffer.</summary>
    public void Write(in Guid value)
    {
        var span = GetSpan(Unsafe.SizeOf<Guid>());
        value.TryWriteBytes(span, Endianness is Endianness.BigEndian, out var bytesWritten);
        Advance(bytesWritten);
    }

    /// <summary>Writes single <see cref="TimeSpan" /> <paramref name="value" /> into buffer.</summary>
    public void Write(in TimeSpan value) => Write(value.Ticks);

    /// <summary>Writes single <see cref="DateTime" /> <paramref name="value" /> into buffer.</summary>
    public void Write(in DateTime value)
    {
        Write((byte)value.Kind);
        Write(value.Ticks);
    }

    /// <summary>Writes single <see cref="DateTimeOffset" /> <paramref name="value" /> into buffer.</summary>
    public void Write(in DateTimeOffset value)
    {
        Write(value.Offset);
        Write(value.Ticks);
    }

    /// <summary>Writes single <see cref="TimeOnly" /> <paramref name="value" /> into buffer.</summary>
    public void Write(in TimeOnly value) => Write(value.Ticks);

    /// <summary>Writes single <see cref="DateOnly" /> <paramref name="value" /> into buffer.</summary>
    public void Write(in DateOnly value) => Write(value.DayNumber);

    /// <summary>Writes single <see cref="Frame" /> <paramref name="value" /> into buffer.</summary>
    public void Write(in Frame value) => WriteAsInt32(in value);

    /// <summary>Writes a span of <see cref="byte" /> <paramref name="value" /> into buffer.</summary>
    public void Write(in ReadOnlySpan<byte> value)
    {
        value.CopyTo(CurrentBuffer);
        Advance(value.Length);
    }

    #region Nullable Values

    /// <inheritdoc cref="Write(in byte)" />
    public void Write(in byte? value)
    {
        Write(value.HasValue);
        if (value.HasValue)
            Write(in Nullable.GetValueRefOrDefaultRef(in value));
    }

    /// <inheritdoc cref="Write(in sbyte)" />
    public void Write(in sbyte? value)
    {
        Write(value.HasValue);
        if (value.HasValue)
            Write(in Nullable.GetValueRefOrDefaultRef(in value));
    }

    /// <inheritdoc cref="Write(in bool)" />
    public void Write(in bool? value)
    {
        Write(value.HasValue);
        if (value.HasValue)
            Write(in Nullable.GetValueRefOrDefaultRef(in value));
    }

    /// <inheritdoc cref="Write(in short)" />
    public void Write(in short? value) => WriteNumber(in value);

    /// <inheritdoc cref="Write(in ushort)" />
    public void Write(in ushort? value) => WriteNumber(in value);

    /// <inheritdoc cref="Write(in int)" />
    public void Write(in int? value) => WriteNumber(in value);

    /// <inheritdoc cref="Write(in uint)" />
    public void Write(in uint? value) => WriteNumber(in value);

    /// <inheritdoc cref="Write(in char)" />
    public void Write(in char? value) => WriteNumber((ushort?)value);

    /// <inheritdoc cref="Write(in long)" />
    public void Write(in long? value) => WriteNumber(in value);

    /// <inheritdoc cref="Write(in ulong)" />
    public void Write(in ulong? value) => WriteNumber(in value);

    /// <inheritdoc cref="Write(in Int128)" />
    public void Write(in Int128? value) => WriteNumber(in value);

    /// <inheritdoc cref="Write(in UInt128)" />
    public void Write(in UInt128? value) => WriteNumber(in value);

    /// <inheritdoc cref="Write(in Half)" />
    public void Write(in Half? value)
    {
        Write(value.HasValue);
        if (value.HasValue)
            Write(in Nullable.GetValueRefOrDefaultRef(in value));
    }

    /// <inheritdoc cref="Write(in float)" />
    public void Write(in float? value)
    {
        Write(value.HasValue);
        if (value.HasValue)
            Write(in Nullable.GetValueRefOrDefaultRef(in value));
    }

    /// <inheritdoc cref="Write(in double)" />
    public void Write(in double? value)
    {
        Write(value.HasValue);
        if (value.HasValue)
            Write(in Nullable.GetValueRefOrDefaultRef(in value));
    }

    /// <inheritdoc cref="Write(in Guid)" />
    public void Write(in Guid? value)
    {
        Write(value.HasValue);
        if (value.HasValue)
            Write(in Nullable.GetValueRefOrDefaultRef(in value));
    }

    /// <inheritdoc cref="Write(in TimeSpan)" />
    public void Write(in TimeSpan? value)
    {
        Write(value.HasValue);
        if (value.HasValue)
            Write(in Nullable.GetValueRefOrDefaultRef(in value));
    }

    /// <inheritdoc cref="Write(in DateTime)" />
    public void Write(in DateTime? value)
    {
        Write(value.HasValue);
        if (value.HasValue)
            Write(in Nullable.GetValueRefOrDefaultRef(in value));
    }

    /// <inheritdoc cref="Write(in DateTimeOffset)" />
    public void Write(in DateTimeOffset? value)
    {
        Write(value.HasValue);
        if (value.HasValue)
            Write(in Nullable.GetValueRefOrDefaultRef(in value));
    }

    /// <inheritdoc cref="Write(in TimeOnly)" />
    public void Write(in TimeOnly? value)
    {
        Write(value.HasValue);
        if (value.HasValue)
            Write(in Nullable.GetValueRefOrDefaultRef(in value));
    }

    /// <inheritdoc cref="Write(in DateOnly)" />
    public void Write(in DateOnly? value)
    {
        Write(value.HasValue);
        if (value.HasValue)
            Write(in Nullable.GetValueRefOrDefaultRef(in value));
    }

    /// <inheritdoc cref="Write(in Frame)" />
    public void Write(in Frame? value)
    {
        Write(value.HasValue);
        if (value.HasValue)
            Write(in Nullable.GetValueRefOrDefaultRef(in value));
    }

    #endregion

    /// <summary>Writes a span of <see cref="sbyte" /> <paramref name="value" /> into buffer.</summary>
    public void Write(in ReadOnlySpan<sbyte> value) => WriteSpan(in value);

    /// <summary>Writes a span of <see cref="bool" /> <paramref name="value" /> into buffer.</summary>
    public void Write(in ReadOnlySpan<bool> value) => WriteSpan(in value);

    /// <summary>Writes a span of <see cref="short" /> <paramref name="value" /> into buffer.</summary>
    public void Write(in ReadOnlySpan<short> value)
    {
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(value, AllocSpan(in value));
        else
            WriteSpan(in value);
    }

    /// <summary>Writes a span of <see cref="ushort" /> <paramref name="value" /> into buffer.</summary>
    public void Write(in ReadOnlySpan<ushort> value)
    {
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(value, AllocSpan(in value));
        else
            WriteSpan(in value);
    }

    /// <summary>Writes a span of <see cref="char" /> <paramref name="value" /> into buffer.</summary>
    public void Write(in ReadOnlySpan<char> value) => Write(MemoryMarshal.Cast<char, ushort>(value));

    /// <summary>Writes a span of <see cref="int" /> <paramref name="value" /> into buffer.</summary>
    public void Write(in ReadOnlySpan<int> value)
    {
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(value, AllocSpan(in value));
        else
            WriteSpan(in value);
    }

    /// <summary>Writes a span of <see cref="uint" /> <paramref name="value" /> into buffer.</summary>
    public void Write(in ReadOnlySpan<uint> value)
    {
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(value, AllocSpan(in value));
        else
            WriteSpan(in value);
    }

    /// <summary>Writes a span of <see cref="long" /> <paramref name="value" /> into buffer.</summary>
    public void Write(in ReadOnlySpan<long> value)
    {
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(value, AllocSpan(in value));
        else
            WriteSpan(in value);
    }

    /// <summary>Writes a span of <see cref="ulong" /> <paramref name="value" /> into buffer.</summary>
    public void Write(in ReadOnlySpan<ulong> value)
    {
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(value, AllocSpan(in value));
        else
            WriteSpan(in value);
    }

    /// <summary>Writes a span of <see cref="Int128" /> <paramref name="value" /> into buffer.</summary>
    public void Write(in ReadOnlySpan<Int128> value)
    {
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(value, AllocSpan(in value));
        else
            WriteSpan(in value);
    }

    /// <summary>Writes a span of <see cref="UInt128" /> <paramref name="value" /> into buffer.</summary>
    public void Write(in ReadOnlySpan<UInt128> value)
    {
        if (Endianness != Platform.Endianness)
            BinaryPrimitives.ReverseEndianness(value, AllocSpan(in value));
        else
            WriteSpan(in value);
    }

    /// <summary>Writes a span of <see cref="float" /> <paramref name="values" /> into buffer.</summary>
    public void Write(in ReadOnlySpan<float> values) => Write(MemoryMarshal.Cast<float, int>(values));

    /// <summary>Writes a span of <see cref="double" /> <paramref name="values" /> into buffer.</summary>
    public void Write(in ReadOnlySpan<double> values) => Write(MemoryMarshal.Cast<double, long>(values));

    /// <summary>Writes a span of <see cref="Half" /> <paramref name="values" /> into buffer.</summary>
    public void Write(in ReadOnlySpan<Half> values) => Write(MemoryMarshal.Cast<Half, short>(values));

    /// <summary>Writes a span of <see cref="Guid" /> <paramref name="values" /> into buffer.</summary>
    public void Write(in ReadOnlySpan<Guid> values)
    {
        if (values.IsEmpty) return;
        ref var current = ref MemoryMarshal.GetReference(values);
        ref var limit = ref Unsafe.Add(ref current, values.Length);

        while (Unsafe.IsAddressLessThan(ref current, ref limit))
        {
            Write(in current);
            current = ref Unsafe.Add(ref current, 1)!;
        }
    }

    /// <summary>Writes a span of <see cref="TimeSpan" /> <paramref name="values" /> into buffer.</summary>
    public void Write(in ReadOnlySpan<TimeSpan> values) => Write(MemoryMarshal.Cast<TimeSpan, long>(values));

    /// <summary>Writes a span of <see cref="TimeOnly" /> <paramref name="values" /> into buffer.</summary>
    public void Write(in ReadOnlySpan<TimeOnly> values) => Write(MemoryMarshal.Cast<TimeOnly, long>(values));

    /// <summary>Writes a span of <see cref="DateTime" /> <paramref name="values" /> into buffer.</summary>
    public void Write(in ReadOnlySpan<DateTime> values)
    {
        if (values.IsEmpty) return;
        ref var current = ref MemoryMarshal.GetReference(values);
        ref var limit = ref Unsafe.Add(ref current, values.Length);

        while (Unsafe.IsAddressLessThan(ref current, ref limit))
        {
            Write(in current);
            current = ref Unsafe.Add(ref current, 1)!;
        }
    }

    /// <summary>Writes a span of <see cref="DateTimeOffset" /> <paramref name="values" /> into buffer.</summary>
    public void Write(in ReadOnlySpan<DateTimeOffset> values)
    {
        if (values.IsEmpty) return;
        ref var current = ref MemoryMarshal.GetReference(values);
        ref var limit = ref Unsafe.Add(ref current, values.Length);

        while (Unsafe.IsAddressLessThan(ref current, ref limit))
        {
            Write(in current);
            current = ref Unsafe.Add(ref current, 1)!;
        }
    }

    /// <summary>Writes a span of <see cref="DateOnly" /> <paramref name="values" /> into buffer.</summary>
    public void Write(in ReadOnlySpan<DateOnly> values) => Write(MemoryMarshal.Cast<DateOnly, int>(values));

    /// <summary>Writes a span of <see cref="Frame" /> <paramref name="values" /> into buffer.</summary>
    public void Write(in ReadOnlySpan<Frame> values) => Write(MemoryMarshal.Cast<Frame, int>(values));

    #region Lists

    /// <summary>Writes a list of <see cref="byte" /> <paramref name="values" /> into buffer.</summary>
    public void Write(in List<byte> values) => Write(GetListSpan(in values));

    /// <summary>Writes a list of <see cref="sbyte" /> <paramref name="values" /> into buffer.</summary>
    public void Write(in List<sbyte> values) => Write(GetListSpan(in values));

    /// <summary>Writes a list of <see cref="bool" /> <paramref name="values" /> into buffer.</summary>
    public void Write(in List<bool> values) => Write(GetListSpan(in values));

    /// <summary>Writes a list of <see cref="short" /> <paramref name="values" /> into buffer.</summary>
    public void Write(in List<short> values) => Write(GetListSpan(in values));

    /// <summary>Writes a list of <see cref="ushort" /> <paramref name="values" /> into buffer.</summary>
    public void Write(in List<ushort> values) => Write(GetListSpan(in values));

    /// <summary>Writes a list of <see cref="char" /> <paramref name="values" /> into buffer.</summary>
    public void Write(in List<char> values) => Write(GetListSpan(in values));

    /// <summary>Writes a list of <see cref="int" /> <paramref name="values" /> into buffer.</summary>
    public void Write(in List<int> values) => Write(GetListSpan(in values));

    /// <summary>Writes a list of <see cref="uint" /> <paramref name="values" /> into buffer.</summary>
    public void Write(in List<uint> values) => Write(GetListSpan(in values));

    /// <summary>Writes a list of <see cref="long" /> <paramref name="values" /> into buffer.</summary>
    public void Write(in List<long> values) => Write(GetListSpan(in values));

    /// <summary>Writes a list of <see cref="ulong" /> <paramref name="values" /> into buffer.</summary>
    public void Write(in List<ulong> values) => Write(GetListSpan(in values));

    /// <summary>Writes a list of <see cref="Int128" /> <paramref name="values" /> into buffer.</summary>
    public void Write(in List<Int128> values) => Write(GetListSpan(in values));

    /// <summary>Writes a list of <see cref="UInt128" /> <paramref name="values" /> into buffer.</summary>
    public void Write(in List<UInt128> values) => Write(GetListSpan(in values));

    /// <summary>Writes a list of <see cref="float" /> <paramref name="values" /> into buffer.</summary>
    public void Write(in List<float> values) => Write(GetListSpan(in values));

    /// <summary>Writes a list of <see cref="double" /> <paramref name="values" /> into buffer.</summary>
    public void Write(in List<double> values) => Write(GetListSpan(in values));

    /// <summary>Writes a list of <see cref="Half" /> <paramref name="values" /> into buffer.</summary>
    public void Write(in List<Half> values) => Write(GetListSpan(in values));

    /// <summary>Writes a list of <see cref="Guid" /> <paramref name="values" /> into buffer.</summary>
    public void Write(in List<Guid> values) => Write(GetListSpan(in values));

    /// <summary>Writes a list of <see cref="TimeSpan" /> <paramref name="values" /> into buffer.</summary>
    public void Write(in List<TimeSpan> values) => Write(GetListSpan(in values));

    /// <summary>Writes a list of <see cref="TimeOnly" /> <paramref name="values" /> into buffer.</summary>
    public void Write(in List<TimeOnly> values) => Write(GetListSpan(in values));

    /// <summary>Writes a list of <see cref="DateTime" /> <paramref name="values" /> into buffer.</summary>
    public void Write(in List<DateTime> values) => Write(GetListSpan(in values));

    /// <summary>Writes a list of <see cref="DateTimeOffset" /> <paramref name="values" /> into buffer.</summary>
    public void Write(in List<DateTimeOffset> values) => Write(GetListSpan(in values));

    /// <summary>Writes a list <see cref="DateOnly" /> <paramref name="values" /> into buffer.</summary>
    public void Write(in List<DateOnly> values) => Write(GetListSpan(in values));

    /// <summary>Writes a list <see cref="Frame" /> <paramref name="values" /> into buffer.</summary>
    public void Write(in List<Frame> values) => Write(GetListSpan(in values));

    #endregion

    /// <summary>Writes an <see cref="string" /> <paramref name="value" /> into buffer as UTF8.</summary>
    public void WriteUtf8String(in ReadOnlySpan<char> value) =>
        Advance(System.Text.Encoding.UTF8.GetBytes(value, CurrentBuffer));

    /// <summary>Writes an unmanaged struct into buffer.</summary>
    public void WriteStruct<T>(in T value) where T : unmanaged
    {
        MemoryMarshal.Write(CurrentBuffer, in value);
        Advance(Unsafe.SizeOf<T>());
    }

    /// <summary>Writes an unmanaged struct span into buffer.</summary>
    public void WriteStruct<T>(ReadOnlySpan<T> values) where T : unmanaged => Write(MemoryMarshal.AsBytes(values));

    /// <summary>Writes an unmanaged struct span into buffer.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteStruct<T>(in T[] values) where T : unmanaged => WriteStruct<T>(values.AsSpan());

    /// <summary>Writes a <see cref="IBinaryInteger{T}" /> <paramref name="value" /> into buffer.</summary>
    /// <typeparam name="T">A numeric type that implements <see cref="IBinaryInteger{T}" />.</typeparam>
    public void WriteNumber<T>(in T value) where T : unmanaged, IBinaryInteger<T>
    {
        ref var valueRef = ref Unsafe.AsRef(in value);
        int size;
        switch (Endianness)
        {
            case Endianness.LittleEndian:
                valueRef.TryWriteLittleEndian(CurrentBuffer, out size);
                break;
            case Endianness.BigEndian:
                valueRef.TryWriteBigEndian(CurrentBuffer, out size);
                break;
            default:
                return;
        }

        Advance(size);
    }

    /// <summary>Writes a <see cref="IBinaryInteger{T}" /> <paramref name="value" /> into buffer.</summary>
    /// <typeparam name="T">A numeric type that implements <see cref="IBinaryInteger{T}" />.</typeparam>
    public void WriteNumber<T>(in T? value) where T : unmanaged, IBinaryInteger<T>
    {
        Write(value.HasValue);
        if (value.HasValue)
            WriteNumber(in Nullable.GetValueRefOrDefaultRef(in value));
    }

    #region WriteAs

    /// <summary>Reinterprets the <paramref name="value" /> as <see cref="byte" /> and writes it into buffer.</summary>
    public void WriteAsByte<T>(in T value) where T : unmanaged =>
        Write(in Unsafe.As<T, byte>(ref Unsafe.AsRef(in value)));

    /// <inheritdoc cref="WriteAsByte{T}(in T)" />
    public void WriteAsByte<T>(in T? value) where T : unmanaged
    {
        Write(value.HasValue);
        if (value.HasValue)
            WriteAsByte(in Nullable.GetValueRefOrDefaultRef(in value));
    }

    /// <inheritdoc cref="WriteAsByte{T}(in T)" />
    public void WriteAsByte<T>(in ReadOnlySpan<T> value) where T : unmanaged =>
        Write(MemoryMarshal.Cast<T, byte>(value));

    /// <inheritdoc cref="WriteAsByte{T}(in T)" />
    public void WriteAsByte<T>(in T[] value) where T : unmanaged =>
        WriteAsByte((ReadOnlySpan<T>)value);

    /// <inheritdoc cref="WriteAsByte{T}(in T)" />
    public void WriteAsByte<T>(in List<T> value) where T : unmanaged => WriteAsByte<T>(GetListSpan(in value));

    /// <summary>Reinterprets the <paramref name="value" /> as <see cref="sbyte" /> and writes it into buffer.</summary>
    public void WriteAsSByte<T>(in T value) where T : unmanaged =>
        Write(in Unsafe.As<T, sbyte>(ref Unsafe.AsRef(in value)));

    /// <inheritdoc cref="WriteAsSByte{T}(in T)" />
    public void WriteAsSByte<T>(in T? value) where T : unmanaged
    {
        Write(value.HasValue);
        if (value.HasValue)
            WriteAsSByte(in Nullable.GetValueRefOrDefaultRef(in value));
    }

    /// <inheritdoc cref="WriteAsSByte{T}(in T)" />
    public void WriteAsSByte<T>(in ReadOnlySpan<T> value) where T : unmanaged =>
        Write(MemoryMarshal.Cast<T, sbyte>(value));

    /// <inheritdoc cref="WriteAsSByte{T}(in T)" />
    public void WriteAsSByte<T>(in T[] value) where T : unmanaged =>
        WriteAsSByte((ReadOnlySpan<T>)value);

    /// <inheritdoc cref="WriteAsSByte{T}(in T)" />
    public void WriteAsSByte<T>(in List<T> value) where T : unmanaged => WriteAsSByte<T>(GetListSpan(in value));

    /// <summary>Reinterprets the <paramref name="value" /> as <see cref="short" /> and writes it into buffer.</summary>
    public void WriteAsInt16<T>(in T value) where T : unmanaged =>
        Write(in Unsafe.As<T, short>(ref Unsafe.AsRef(in value)));

    /// <inheritdoc cref="WriteAsInt16{T}(in T)" />
    public void WriteAsInt16<T>(in T? value) where T : unmanaged
    {
        Write(value.HasValue);
        if (value.HasValue)
            WriteAsInt16(in Nullable.GetValueRefOrDefaultRef(in value));
    }

    /// <inheritdoc cref="WriteAsInt16{T}(in T)" />
    public void WriteAsInt16<T>(in ReadOnlySpan<T> value) where T : unmanaged =>
        Write(MemoryMarshal.Cast<T, short>(value));

    /// <inheritdoc cref="WriteAsInt16{T}(in T)" />
    public void WriteAsInt16<T>(in T[] value) where T : unmanaged =>
        WriteAsInt16((ReadOnlySpan<T>)value);

    /// <inheritdoc cref="WriteAsInt16{T}(in T)" />
    public void WriteAsInt16<T>(in List<T> value) where T : unmanaged => WriteAsInt16<T>(GetListSpan(in value));

    /// <summary>Reinterprets the <paramref name="value" /> as <see cref="ushort" /> and writes it into buffer.</summary>
    public void WriteAsUInt16<T>(in T value) where T : unmanaged =>
        Write(in Unsafe.As<T, ushort>(ref Unsafe.AsRef(in value)));

    /// <inheritdoc cref="WriteAsUInt16{T}(in T)" />
    public void WriteAsUInt16<T>(in T? value) where T : unmanaged
    {
        Write(value.HasValue);
        if (value.HasValue)
            WriteAsUInt16(in Nullable.GetValueRefOrDefaultRef(in value));
    }

    /// <inheritdoc cref="WriteAsUInt16{T}(in T)" />
    public void WriteAsUInt16<T>(in ReadOnlySpan<T> value) where T : unmanaged =>
        Write(MemoryMarshal.Cast<T, ushort>(value));

    /// <inheritdoc cref="WriteAsUInt16{T}(in T)" />
    public void WriteAsUInt16<T>(in T[] value) where T : unmanaged =>
        WriteAsUInt16((ReadOnlySpan<T>)value);

    /// <inheritdoc cref="WriteAsUInt16{T}(in T)" />
    public void WriteAsUInt16<T>(in List<T> value) where T : unmanaged => WriteAsUInt16<T>(GetListSpan(in value));

    /// <summary>Reinterprets the <paramref name="value" /> as <see cref="int" /> and writes it into buffer.</summary>
    public void WriteAsInt32<T>(in T value) where T : unmanaged =>
        Write(in Unsafe.As<T, int>(ref Unsafe.AsRef(in value)));

    /// <inheritdoc cref="WriteAsInt32{T}(in T)" />
    public void WriteAsInt32<T>(in T? value) where T : unmanaged
    {
        Write(value.HasValue);
        if (value.HasValue)
            WriteAsInt32(in Nullable.GetValueRefOrDefaultRef(in value));
    }

    /// <inheritdoc cref="WriteAsInt32{T}(in T)" />
    public void WriteAsInt32<T>(in ReadOnlySpan<T> value) where T : unmanaged =>
        Write(MemoryMarshal.Cast<T, int>(value));

    /// <inheritdoc cref="WriteAsInt32{T}(in T)" />
    public void WriteAsInt32<T>(in T[] value) where T : unmanaged =>
        WriteAsInt32((ReadOnlySpan<T>)value);

    /// <inheritdoc cref="WriteAsInt32{T}(in T)" />
    public void WriteAsInt32<T>(in List<T> value) where T : unmanaged => WriteAsInt32<T>(GetListSpan(in value));

    /// <summary>Reinterprets the <paramref name="value" /> as <see cref="uint" /> and writes it into buffer.</summary>
    public void WriteAsUInt32<T>(in T value) where T : unmanaged =>
        Write(in Unsafe.As<T, uint>(ref Unsafe.AsRef(in value)));

    /// <inheritdoc cref="WriteAsUInt32{T}(in T)" />
    public void WriteAsUInt32<T>(in T? value) where T : unmanaged
    {
        Write(value.HasValue);
        if (value.HasValue)
            WriteAsUInt32(in Nullable.GetValueRefOrDefaultRef(in value));
    }

    /// <inheritdoc cref="WriteAsUInt32{T}(in T)" />
    public void WriteAsUInt32<T>(in ReadOnlySpan<T> value) where T : unmanaged =>
        Write(MemoryMarshal.Cast<T, uint>(value));

    /// <inheritdoc cref="WriteAsUInt32{T}(in T)" />
    public void WriteAsUInt32<T>(in T[] value) where T : unmanaged =>
        WriteAsUInt32((ReadOnlySpan<T>)value);

    /// <inheritdoc cref="WriteAsUInt32{T}(in T)" />
    public void WriteAsUInt32<T>(in List<T> value) where T : unmanaged => WriteAsUInt32<T>(GetListSpan(in value));


    /// <summary>Reinterprets the <paramref name="value" /> as <see cref="long" /> and writes it into buffer.</summary>
    public void WriteAsInt64<T>(in T value) where T : unmanaged =>
        Write(in Unsafe.As<T, long>(ref Unsafe.AsRef(in value)));

    /// <inheritdoc cref="WriteAsInt64{T}(in T)" />
    public void WriteAsInt64<T>(in T? value) where T : unmanaged
    {
        Write(value.HasValue);
        if (value.HasValue)
            WriteAsInt64(in Nullable.GetValueRefOrDefaultRef(in value));
    }

    /// <inheritdoc cref="WriteAsInt64{T}(in T)" />
    public void WriteAsInt64<T>(in ReadOnlySpan<T> value) where T : unmanaged =>
        Write(MemoryMarshal.Cast<T, long>(value));

    /// <inheritdoc cref="WriteAsInt64{T}(in T)" />
    public void WriteAsInt64<T>(in T[] value) where T : unmanaged =>
        WriteAsInt64((ReadOnlySpan<T>)value);

    /// <inheritdoc cref="WriteAsInt64{T}(in T)" />
    public void WriteAsInt64<T>(in List<T> value) where T : unmanaged => WriteAsInt64<T>(GetListSpan(in value));

    /// <summary>Reinterprets the <paramref name="value" /> as <see cref="ulong" /> and writes it into buffer.</summary>
    public void WriteAsUInt64<T>(in T value) where T : unmanaged =>
        Write(in Unsafe.As<T, ulong>(ref Unsafe.AsRef(in value)));

    /// <inheritdoc cref="WriteAsUInt64{T}(in T)" />
    public void WriteAsUInt64<T>(in T? value) where T : unmanaged
    {
        Write(value.HasValue);
        if (value.HasValue)
            WriteAsUInt64(in Nullable.GetValueRefOrDefaultRef(in value));
    }

    /// <inheritdoc cref="WriteAsUInt64{T}(in T)" />
    public void WriteAsUInt64<T>(in ReadOnlySpan<T> value) where T : unmanaged =>
        Write(MemoryMarshal.Cast<T, ulong>(value));

    /// <inheritdoc cref="WriteAsUInt64{T}(in T)" />
    public void WriteAsUInt64<T>(in T[] value) where T : unmanaged =>
        WriteAsUInt64((ReadOnlySpan<T>)value);

    /// <inheritdoc cref="WriteAsUInt64{T}(in T)" />
    public void WriteAsUInt64<T>(in List<T> value) where T : unmanaged => WriteAsUInt64<T>(GetListSpan(in value));

    #endregion
}
