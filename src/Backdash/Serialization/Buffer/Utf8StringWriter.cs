namespace Backdash.Serialization.Buffer;

using System.Text;

readonly ref struct Utf8StringWriter
{
    readonly Span<byte> buffer;
    readonly ref int offset;

    public Utf8StringWriter(in Span<byte> bufferArg, ref int offset)
    {
        buffer = bufferArg;
        this.offset = ref offset;
    }

    public bool WriteChars(ReadOnlySpan<char> value)
    {
        Span<byte> dest = buffer[offset..];
        if (dest.IsEmpty) return false;

        var size = Encoding.UTF8.GetByteCount(value);

        var chars = size <= dest.Length
            ? value
            : value[..dest.Length];

        offset += Encoding.UTF8.GetBytes(chars, dest);
        return true;
    }

    public bool Write(ReadOnlySpan<byte> value)
    {
        Span<byte> dest = buffer[offset..];
        if (dest.IsEmpty) return false;

        var bytes = value.Length <= dest.Length
            ? value
            : value[..dest.Length];

        bytes.CopyTo(dest);
        offset += value.Length;
        return true;
    }

    public bool Write<T>(T value, ReadOnlySpan<char> format) where T : IUtf8SpanFormattable
    {
        Span<byte> dest = buffer[offset..];
        if (dest.IsEmpty) return false;

        if (!value.TryFormat(dest, out int written, format, null))
            return false;

        offset += written;
        return true;
    }

    public bool Write<T>(T value) where T : IUtf8SpanFormattable => Write(value, []);

    const int MaxLocalStringSize = 24;

    public bool WriteFormat<T>(T value, ReadOnlySpan<char> format = default) where T : ISpanFormattable
    {
        Span<byte> dest = buffer[offset..];
        if (dest.IsEmpty) return false;
        Span<char> charBuffer = stackalloc char[MaxLocalStringSize];
        return value.TryFormat(charBuffer, out int written, format, null) && WriteChars(charBuffer[..written]);
    }

    public bool WriteEnum<T>(T value, ReadOnlySpan<char> format = default) where T : struct, Enum
    {
        Span<byte> dest = buffer[offset..];
        if (dest.IsEmpty) return false;

        Span<char> charBuffer = stackalloc char[MaxLocalStringSize];
        if (!Enum.TryFormat(value, charBuffer, out int written, format))
            return false;

        offset += written;
        return true;
    }
}
