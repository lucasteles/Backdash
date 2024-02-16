using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Backdash.Core;
using Backdash.Data;
using Backdash.Serialization.Buffer;

namespace Backdash.Sync;

struct GameInput : IEquatable<GameInput>, IUtf8SpanFormattable
{
    public Frame Frame = Frame.Null;

    public GameInputBuffer Buffer;

    public int Size;

    public static GameInput CreateEmpty() => new();

    public static GameInput Create(int size) => new(size);

    public static GameInput Create(int size, Frame frame) => new(size)
    {
        Frame = frame,
    };

    public GameInput(in GameInputBuffer inputBuffer, int size)
    {
        ReadOnlySpan<byte> bits = inputBuffer;
        Trace.Assert(bits.Length <= Max.TotalInputSizeInBytes);
        Trace.Assert(bits.Length > 0);
        Size = size;
        Buffer = inputBuffer;
    }

    public GameInput(int size) : this(new(), size) { }

    public GameInput() : this(0) { }

    public GameInput(ReadOnlySpan<byte> bits) : this(new GameInputBuffer(bits), bits.Length) { }
    public void IncrementFrame() => Frame = Frame.Next();
    public void ResetFrame() => Frame = Frame.Null;

#pragma warning disable IDE0251
    public void Erase() => Mem.Clear(Buffer);
#pragma warning restore IDE0251

    public readonly void CopyTo(Span<byte> bytes) => Buffer[..Size].CopyTo(bytes);

    public readonly string ToString(string? format, IFormatProvider? formatProvider)
    {
        StringBuilder builder = new();
        builder.Append($"{{ Frame: {Frame.Number},");
        builder.Append($" Size: {Size}, Input: ");
        builder.Append(Buffer.ToString());
        builder.Append(" }");
        return builder.ToString();
    }

    public readonly bool TryFormat(
        Span<byte> utf8Destination,
        out int bytesWritten, ReadOnlySpan<char> format,
        IFormatProvider? provider
    )
    {
        bytesWritten = 0;
        Utf8StringWriter writer = new(in utf8Destination, ref bytesWritten);

        if (!writer.Write("{Frame: "u8)) return false;
        if (!writer.Write(Frame.Number)) return false;
        if (!writer.Write(", Size: "u8)) return false;
        if (!writer.Write(Size)) return false;
        if (!writer.Write("}"u8)) return false;
        return true;
    }

    public readonly bool Equals(GameInput other) =>
        Frame == other.Frame
        && Size == other.Size
        && Mem.SpanEqual<byte>(Buffer, other.Buffer, truncate: true);

    public override readonly bool Equals(object? obj) => obj is GameInput gi && Equals(gi);

    // ReSharper disable once BaseObjectGetHashCodeCallInGetHashCode
    public override readonly int GetHashCode() => base.GetHashCode();

    public static bool operator ==(GameInput a, GameInput b) => a.Equals(b);
    public static bool operator !=(GameInput a, GameInput b) => !(a == b);
}

[InlineArray(Max.TotalInputSizeInBytes), Serializable]
public struct GameInputBuffer
{
    byte element0;

    public GameInputBuffer(ReadOnlySpan<byte> bits) => bits.CopyTo(this);

    public readonly string ToString(bool trimZeros) =>
        Mem.GetBitString(this, splitAt: Max.InputSizeInBytes, trimRightZeros: trimZeros);

    public override readonly string ToString() => ToString(trimZeros: true);
}
