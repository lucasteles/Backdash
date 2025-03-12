using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Backdash.Data;

namespace Backdash.Core;

static class Mem
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Clear(in Span<byte> bytes) => bytes.Clear();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<T> AsSpan<T>(ref readonly T data) where T : unmanaged => new(in data);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<byte> AsBytes<T>(scoped ref readonly T data) where T : unmanaged =>
        MemoryMarshal.AsBytes(new Span<T>(ref Unsafe.AsRef(in data)));

    public static bool ByteEqual(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right, bool truncate = false)
    {
        if (!truncate) return right.SequenceEqual(left);
        var minLength = Math.Min(left.Length, right.Length);
        return right[..minLength].SequenceEqual(left[..minLength]);
    }

    public static byte[] AllocatePinnedArray(int size)
    {
        var buffer = GC.AllocateArray<byte>(length: size, pinned: true);
        Array.Clear(buffer);
        return buffer;
    }

    public static Memory<byte> AllocatePinnedMemory(int size)
    {
        var buffer = AllocatePinnedArray(size);
        return MemoryMarshal.CreateFromPinnedArray(buffer, 0, buffer.Length);
    }

    public static int GetHashCode<T>(ReadOnlySpan<T> values) where T : unmanaged
    {
        HashCode hash = new();
        hash.AddBytes(MemoryMarshal.AsBytes(values));
        return hash.ToHashCode();
    }

    public static bool IsUnsigned<T>() where T : IBinaryInteger<T>, IMinMaxValue<T> =>
        T.IsZero(T.MinValue);

    public static string GetBitString(
        in ReadOnlySpan<byte> bytes,
        bool trimRightZeros = true,
        int bytePad = ByteSize.ByteToBits
    )
    {
        StringBuilder builder = new(bytes.Length * bytePad * sizeof(char));
        const char byteSep = '-';
        Span<char> binary = stackalloc char[bytePad];
        for (var i = 0; i < bytes.Length; i++)
        {
            if (i > 0)
                builder.Append(byteSep);
            binary.Clear();
            var base10 = bytes[i];
            var binSize = Math.Clamp((int)Math.Ceiling(Math.Log(base10 + 1, 2)), 1, bytePad);
            var padSize = bytePad - binSize;
            binary[..padSize].Fill('0');
            for (var j = binSize - 1; j >= 0; j--)
            {
                binary[padSize + j] = base10 % 2 is 0 ? '0' : '1';
                base10 /= 2;
            }

            builder.Append(binary);
        }

        if (!trimRightZeros)
            return builder.ToString();
        int lastNonZero;
        int lastByteSep = builder.Length;
        for (lastNonZero = builder.Length - 1; lastNonZero >= 0; lastNonZero--)
        {
            if (builder[lastNonZero] is byteSep)
                lastByteSep = lastNonZero;
            if (builder[lastNonZero] is not ('0' or byteSep))
                break;
        }

        lastNonZero = Math.Max(lastNonZero, lastByteSep);
        var trimmed = builder.Remove(lastNonZero, builder.Length - lastNonZero);
        return trimmed.ToString();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsUnmanagedStruct<T>() => !RuntimeHelpers.IsReferenceOrContainsReferences<T>();

    public static int PopCount<T>(in T[] values) where T : unmanaged => PopCount<T>(values.AsSpan());

    public static int PopCount<T>(in ReadOnlySpan<T> values) where T : unmanaged
    {
#pragma warning disable S907
        var count = 0;
        var bytes = MemoryMarshal.AsBytes(values);
        ref var current = ref MemoryMarshal.GetReference(bytes);
        ref var limit = ref Unsafe.Add(ref current, bytes.Length);
        ref var next = ref current;
        while (Unsafe.IsAddressLessThan(ref current, ref limit))
        {
            next = ref Unsafe.Add(ref current, sizeof(ulong) - 1);
            if (Unsafe.IsAddressLessThan(ref next, ref limit))
            {
                var value = Unsafe.ReadUnaligned<ulong>(ref current);
                count += BitOperations.PopCount(value);
                current = ref next;
                goto LOOP;
            }

            next = ref Unsafe.Add(ref current, sizeof(uint) - 1);
            if (Unsafe.IsAddressLessThan(ref next, ref limit))
            {
                var value = Unsafe.ReadUnaligned<uint>(ref current);
                count += BitOperations.PopCount(value);
                current = ref next;
                goto LOOP;
            }

            next = ref Unsafe.Add(ref current, sizeof(ushort) - 1);
            if (Unsafe.IsAddressLessThan(ref next, ref limit))
            {
                var value = Unsafe.ReadUnaligned<ushort>(ref current);
                count += ushort.PopCount(value);
                current = ref next;
                goto LOOP;
            }

            count += byte.PopCount(current);
LOOP:
            current = ref Unsafe.Add(ref current, 1);
        }

        return count;
#pragma warning restore S907
    }
}
