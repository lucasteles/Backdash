using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using nGGPO.Data;

namespace nGGPO.Utils;

static class Mem
{
    const int MaxStackLimit = 1024;

    public static void Clear(in Span<byte> bytes) => bytes.Clear();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TValue ReadStruct<TValue>(in ReadOnlySpan<byte> bytes)
        where TValue : struct =>
        MemoryMarshal.Read<TValue>(bytes);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int WriteStruct<TValue>(scoped in TValue value, Span<byte> destination)
        where TValue : struct
    {
        MemoryMarshal.Write(destination, in value);
        return Unsafe.SizeOf<TValue>();
    }

    public static T ReadUnaligned<T>(in ReadOnlySpan<byte> data) where T : struct
    {
        if (data.Length < Unsafe.SizeOf<T>())
            throw new ArgumentOutOfRangeException(nameof(data));

        return Unsafe.ReadUnaligned<T>(ref MemoryMarshal.GetReference(data));
    }

    public static TInt EnumAsInteger<TEnum, TInt>(TEnum enumValue)
        where TEnum : unmanaged, Enum
        where TInt : unmanaged, IBinaryInteger<TInt>
    {
        if (Unsafe.SizeOf<TEnum>() != Unsafe.SizeOf<TInt>()) throw new NggpoException("type mismatch");
        return Unsafe.As<TEnum, TInt>(ref enumValue);
    }

    public static TEnum IntegerAsEnum<TEnum, TInt>(TInt intValue)
        where TEnum : unmanaged, Enum
        where TInt : unmanaged, IBinaryInteger<TInt>
    {
        if (Unsafe.SizeOf<TEnum>() != Unsafe.SizeOf<TInt>()) throw new NggpoException("type mismatch");
        return Unsafe.As<TInt, TEnum>(ref intValue);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsValidSizeOnStack<T>(int maxSize = 0) where T : struct
    {
        var size = Unsafe.SizeOf<T>();
        return size <= MaxStackLimit && (maxSize <= 0 || size < maxSize);
    }

    public static bool SpanEqual<T>(
        ReadOnlySpan<T> you,
        ReadOnlySpan<T> me,
        bool truncate = false
    ) =>
        you.Length <= me.Length && me.SequenceEqual(truncate ? you[..me.Length] : you);

    public static unsafe int MarshallStruct<T>(in T message, in Span<byte> body)
        where T : struct
    {
        var size = Marshal.SizeOf(message);

        nint ptr;

        if (size > MaxStackLimit)
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
            if (size > MaxStackLimit)
                Marshal.FreeHGlobal(ptr);
        }

        return size;
    }

    public static unsafe T UnmarshallStruct<T>(in ReadOnlySpan<byte> body) where T : struct
    {
        var size = body.Length;

        nint ptr;

        if (size > MaxStackLimit)
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
            if (size > MaxStackLimit)
                Marshal.FreeHGlobal(ptr);
        }
    }

    public static string JoinString<T>(
        in ReadOnlySpan<T> values,
        ReadOnlySpan<char> separator = default,
        ReadOnlySpan<char> prefix = default,
        ReadOnlySpan<char> suffix = default
    )
    {
        separator = separator.IsEmpty ? ", " : separator;
        prefix = prefix.IsEmpty ? "[" : separator;
        suffix = suffix.IsEmpty ? "]" : suffix;

        StringBuilder builder = new();
        builder.Append(prefix);

        for (var i = 0; i < values.Length; i++)
        {
            if (i > 0) builder.Append(separator);
            builder.Append(values[i]);
        }

        builder.Append(suffix);
        return builder.ToString();
    }

    // TODO: create non alloc version of this
    public static string GetBitString(
        in ReadOnlySpan<byte> bytes,
        int splitAt = 0,
        int bytePad = ByteSize.ByteToBits
    )
    {
        StringBuilder builder = new();

        for (var i = 0; i < bytes.Length; i++)
        {
            if (i > 0)
                if (splitAt > 0 && i % splitAt is 0) builder.Append('|');
                else builder.Append('-');

            var bin = Convert.ToString(bytes[i], 2).PadLeft(bytePad, '0');

            for (var j = 0; j < bin.Length; j++)
                builder.Append(bin[j]);
        }

        return builder.ToString();
    }
}
