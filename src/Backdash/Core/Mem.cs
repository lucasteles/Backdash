using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Backdash.Data;

namespace Backdash.Core;

static class Mem
{
    public const int MaxStackLimit = 1024;
    public static void Clear(in Span<byte> bytes) => bytes.Clear();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TValue ReadStruct<TValue>(in ReadOnlySpan<byte> bytes) where TValue : struct =>
        MemoryMarshal.Read<TValue>(bytes);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteStruct<TValue>(in TValue value, Span<byte> destination) where TValue : struct
    {
        MemoryMarshal.Write(destination, in value);
        return Unsafe.SizeOf<TValue>();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<byte> GetSpan<T>(scoped ref T data) where T : struct
    {
        ThrowHelpers.ThrowIfTypeIsReferenceOrContainsReferences<T>();
        return MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref data, 1));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref TInt EnumAsInteger<TEnum, TInt>(ref TEnum enumValue)
        where TEnum : unmanaged, Enum
        where TInt : unmanaged, IBinaryInteger<TInt>
    {
        if (Unsafe.SizeOf<TEnum>() != Unsafe.SizeOf<TInt>()) throw new NetcodeException("type mismatch");
        return ref Unsafe.As<TEnum, TInt>(ref enumValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref TEnum IntegerAsEnum<TEnum, TInt>(ref TInt intValue)
        where TEnum : unmanaged, Enum
        where TInt : unmanaged, IBinaryInteger<TInt>
    {
        if (Unsafe.SizeOf<TEnum>() != Unsafe.SizeOf<TInt>()) throw new NetcodeException("type mismatch");
        return ref Unsafe.As<TInt, TEnum>(ref intValue);
    }

    public static bool EqualBytes(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right, bool truncate = false)
    {
        if (!truncate) return right.SequenceEqual(left);
        var minLength = Math.Min(left.Length, right.Length);
        return right[..minLength].SequenceEqual(left[..minLength]);
    }

#if !AOT_ENABLED
    public static unsafe int MarshallStruct<T>(in T message, in Span<byte> body)
        where T : struct
    {
        var size = Marshal.SizeOf(message);
        nint ptr;
        bool fitStack = size <= MaxStackLimit;
        if (!fitStack)
            ptr = Marshal.AllocHGlobal(size);
        else
        {
            var stackPointer = stackalloc byte[size];
            ptr = (nint)stackPointer;
        }

        try
        {
            fixed (byte* bodyPtr = body)
            {
                Marshal.StructureToPtr(message, ptr, true);
                Span<byte> source = new((void*)ptr, size);
                Span<byte> dest = new(bodyPtr, size);
                source.CopyTo(dest);
            }
        }
        finally
        {
            if (!fitStack)
                Marshal.FreeHGlobal(ptr);
        }

        return size;
    }

    public static unsafe T UnmarshallStruct<T>(in ReadOnlySpan<byte> body) where T : struct
    {
        var size = body.Length;
        nint ptr;
        bool fitStack = size <= MaxStackLimit;
        if (!fitStack)
            ptr = Marshal.AllocHGlobal(size);
        else
        {
            var stackPointer = stackalloc byte[size];
            ptr = (nint)stackPointer;
        }

        try
        {
            Span<byte> dest = new((void*)ptr, body.Length);
            body.CopyTo(dest);
            return Marshal.PtrToStructure<T>(ptr);
        }
        finally
        {
            if (!fitStack)
                Marshal.FreeHGlobal(ptr);
        }
    }
#endif

    public static byte[] AllocatePinnedArray(int size)
    {
        var buffer = GC.AllocateArray<byte>(length: size, pinned: true);
        Array.Clear(buffer);
        return buffer;
    }

    public static Memory<byte> CreatePinnedMemory(int size)
    {
        var buffer = AllocatePinnedArray(size);
        return MemoryMarshal.CreateFromPinnedArray(buffer, 0, buffer.Length);
    }

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
    public static int SizeOf<TInput>() where TInput : unmanaged => Unsafe.SizeOf<TInput>();

    public static bool IsReferenceOrContainsReferences<T>() => RuntimeHelpers.IsReferenceOrContainsReferences<T>();
}
