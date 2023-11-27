using System.Buffers.Binary;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace nGGPO.Network;

public static class Endianness
{
    public static readonly bool IsLittleEndian = BitConverter.IsLittleEndian;

    public static char ToHost(char value) => IsLittleEndian
        ? (char)BinaryPrimitives.ReverseEndianness(value)
        : value;

    public static void ToHost(ReadOnlySpan<char> value, Span<char> destination)
    {
        if (!IsLittleEndian) return;
        var ushortSpan = MemoryMarshal.Cast<char, ushort>(value);
        var ushortDest = MemoryMarshal.Cast<char, ushort>(destination);
        BinaryPrimitives.ReverseEndianness(ushortSpan, ushortDest);
    }

    public static void ToHost(Span<char> value) => ToHost(value, value);

    public static short ToHost(short value) => IsLittleEndian
        ? BinaryPrimitives.ReverseEndianness(value)
        : value;

    public static void ToHost(ReadOnlySpan<short> value, Span<short> destination)
    {
        if (IsLittleEndian) BinaryPrimitives.ReverseEndianness(value, destination);
    }

    public static void ToHost(Span<short> value) => ToHost(value, value);

    public static ushort ToHost(ushort value) => IsLittleEndian
        ? BinaryPrimitives.ReverseEndianness(value)
        : value;

    public static void ToHost(ReadOnlySpan<ushort> value, Span<ushort> destination)
    {
        if (IsLittleEndian) BinaryPrimitives.ReverseEndianness(value, destination);
    }

    public static void ToHost(Span<ushort> value) => ToHost(value, value);

    public static int ToHost(int value) => IsLittleEndian
        ? BinaryPrimitives.ReverseEndianness(value)
        : value;

    public static void ToHost(ReadOnlySpan<int> value, Span<int> destination)
    {
        if (IsLittleEndian) BinaryPrimitives.ReverseEndianness(value, destination);
    }

    public static void ToHost(Span<int> value) => ToHost(value, value);

    public static uint ToHost(uint value) => IsLittleEndian
        ? BinaryPrimitives.ReverseEndianness(value)
        : value;

    public static void ToHost(ReadOnlySpan<uint> value, Span<uint> destination)
    {
        if (IsLittleEndian) BinaryPrimitives.ReverseEndianness(value, destination);
    }

    public static void ToHost(Span<uint> value) => ToHost(value, value);

    public static long ToHost(long value) => IsLittleEndian
        ? BinaryPrimitives.ReverseEndianness(value)
        : value;

    public static void ToHost(ReadOnlySpan<long> value, Span<long> destination)
    {
        if (IsLittleEndian) BinaryPrimitives.ReverseEndianness(value, destination);
    }

    public static void ToHost(Span<long> value) => ToHost(value, value);

    public static ulong ToHost(ulong value) => IsLittleEndian
        ? BinaryPrimitives.ReverseEndianness(value)
        : value;

    public static void ToHost(ReadOnlySpan<ulong> value, Span<ulong> destination)
    {
        if (IsLittleEndian) BinaryPrimitives.ReverseEndianness(value, destination);
    }

    public static void ToHost(Span<ulong> value) => ToHost(value, value);

    public static Int128 ToHost(Int128 value) => IsLittleEndian
        ? BinaryPrimitives.ReverseEndianness(value)
        : value;

    public static void ToHost(ReadOnlySpan<Int128> value, Span<Int128> destination)
    {
        if (IsLittleEndian) BinaryPrimitives.ReverseEndianness(value, destination);
    }

    public static void ToHost(Span<Int128> value) => ToHost(value, value);

    public static UInt128 ToHost(UInt128 value) => IsLittleEndian
        ? BinaryPrimitives.ReverseEndianness(value)
        : value;

    public static void ToHost(ReadOnlySpan<UInt128> value, Span<UInt128> destination)
    {
        if (IsLittleEndian) BinaryPrimitives.ReverseEndianness(value, destination);
    }

    public static void ToHost(Span<UInt128> value) => ToHost(value, value);

    static TTo As<TFrom, TTo>(in TFrom value) where TFrom : struct where TTo : struct
    {
        var valueRef = value;
        return Unsafe.As<TFrom, TTo>(ref valueRef);
    }

    public static T ToHostOrder<T>(T value) where T : unmanaged =>
        value switch
        {
            char n => As<char, T>(ToHost(n)),
            short n => As<short, T>(ToHost(n)),
            ushort n => As<ushort, T>(ToHost(n)),
            int n => As<int, T>(ToHost(n)),
            uint n => As<uint, T>(ToHost(n)),
            long n => As<long, T>(ToHost(n)),
            ulong n => As<ulong, T>(ToHost(n)),
            Int128 n => As<Int128, T>(ToHost(n)),
            UInt128 n => As<UInt128, T>(ToHost(n)),
            _ => value,
        };

    #region HostToNetwork

    public static char ToNetwork(char value) => ToHost(value);
    public static short ToNetwork(short value) => ToHost(value);
    public static ushort ToNetwork(ushort value) => ToHost(value);
    public static int ToNetwork(int value) => ToHost(value);
    public static uint ToNetwork(uint value) => ToHost(value);
    public static long ToNetwork(long value) => ToHost(value);
    public static ulong ToNetwork(ulong value) => ToHost(value);
    public static Int128 ToNetwork(Int128 value) => ToHost(value);
    public static UInt128 ToNetwork(UInt128 value) => ToHost(value);

    public static void ToNetwork(ReadOnlySpan<char> value, Span<char> destination) =>
        ToHost(value, destination);

    public static void ToNetwork(ReadOnlySpan<short> value, Span<short> destination) =>
        ToHost(value, destination);

    public static void ToNetwork(ReadOnlySpan<ushort> value, Span<ushort> destination) =>
        ToHost(value, destination);

    public static void ToNetwork(ReadOnlySpan<int> value, Span<int> destination) =>
        ToHost(value, destination);

    public static void ToNetwork(ReadOnlySpan<uint> value, Span<uint> destination) =>
        ToHost(value, destination);

    public static void ToNetwork(ReadOnlySpan<long> value, Span<long> destination) =>
        ToHost(value, destination);

    public static void ToNetwork(ReadOnlySpan<ulong> value, Span<ulong> destination) =>
        ToHost(value, destination);

    public static void ToNetwork(ReadOnlySpan<Int128> value, Span<Int128> destination) =>
        ToHost(value, destination);

    public static void ToNetwork(ReadOnlySpan<UInt128> value, Span<UInt128> destination) =>
        ToHost(value, destination);

    public static T ToNetworkOrder<T>(T value) where T : unmanaged => ToHostOrder(value);

    #endregion
}