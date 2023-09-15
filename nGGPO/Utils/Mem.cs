using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace nGGPO.Utils;

public static class Mem
{
    public const int ByteSize = 8;

    public static IMemoryOwner<T> Rent<T>(int size) => MemoryPool<T>.Shared.Rent(size);
    public static IMemoryOwner<byte> Rent(int size) => Rent<byte>(size);

    public static void Return<T>(T[] arr, bool clearArray = false) =>
        ArrayPool<T>.Shared.Return(arr, clearArray);

    public static bool BytesEqual(ReadOnlySpan<byte> a1, ReadOnlySpan<byte> a2) =>
        a1.Length == a2.Length && a1.SequenceEqual(a2);

    public static IMemoryOwner<byte> StructToBytes<T>(T message) where T : struct
    {
        var size = Marshal.SizeOf(message);
        var buffer = Rent(size);
        MemoryMarshal.Write(buffer.Memory.Span, ref message);
        return buffer;
    }

    public static T BytesToStruct<T>(ReadOnlySpan<byte> body) where T : struct =>
        MemoryMarshal.Read<T>(body);

    public static TInt EnumAsInteger<TEnum, TInt>(TEnum enumValue)
        where TEnum : unmanaged, Enum
        where TInt : unmanaged
    {
        if (Unsafe.SizeOf<TEnum>() != Unsafe.SizeOf<TInt>()) throw new Exception("type mismatch");
        return Unsafe.As<TEnum, TInt>(ref enumValue);
    }

    public static TEnum IntegerAsEnum<TEnum, TInt>(TInt intValue)
        where TEnum : unmanaged, Enum
        where TInt : unmanaged
    {
        if (Unsafe.SizeOf<TEnum>() != Unsafe.SizeOf<TInt>()) throw new Exception("type mismatch");
        return Unsafe.As<TInt, TEnum>(ref intValue);
    }

    public static string GetBitString(
        in ReadOnlySpan<byte> bytes,
        int splitAt = 0,
        int bytePad = ByteSize)
    {
        var builder = new StringBuilder();

        for (var i = 0; i < bytes.Length; i++)
        {
            if (i > 0 && splitAt > 0 && i % splitAt is 0) builder.Append('-');

            var bin = Convert.ToString(bytes[i], 2).PadLeft(bytePad, '0');
            for (var j = bin.Length - 1; j >= 0; j--)
                builder.Append(bin[j]);
        }

        return builder.ToString();
    }
}