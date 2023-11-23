﻿using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Text;
using nGGPO.DataStructure;
using nGGPO.Utils;

namespace nGGPO.Input;

[InlineArray(Max.InputBytes * Max.Players)]
public struct GameInputBuffer
{
    byte element0;
}

struct GameInput : IEquatable<GameInput>
{
    public Frame Frame { get; private set; } = Frame.Null;
    public static GameInput Empty => new();

    GameInputBuffer buffer = new();
    public int Size { get; set; }

    public GameInput(ReadOnlySpan<byte> bits)
    {
        Tracer.Assert(bits.Length <= Max.InputBytes * Max.Players);
        Tracer.Assert(bits.Length > 0);
        Size = bits.Length;
        CopyFrom(bits);
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> AsSpan() => Mem.InlineArrayAsSpan<GameInputBuffer, byte>(ref buffer, Size);

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlySpan<byte> AsReadOnlySpan() =>
        Mem.InlineArrayAsReadOnlySpan<GameInputBuffer, byte>(in buffer, Size);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BitVector GetBitVector()
    {
        var span = AsSpan();
        return new(ref span);
    }

    public void CopyFrom(in ReadOnlySpan<byte> bits, int offset = 0) =>
        bits.CopyTo(AsSpan()[offset..]);

    public bool IsEmpty => Size is 0;
    public void IncrementFrame() => Frame = Frame.Next;
    public void SetFrame(Frame frame) => Frame = frame;
    public void ResetFrame() => Frame = Frame.Null;
    public void Clear() => AsSpan().Clear();

    public override string ToString()
    {
        var builder = new StringBuilder();

        builder.Append($"{{ Frame: {Frame},");
        builder.Append($" Size: {Size}, Input: ");

        builder.Append(GetBitVector().ToString(splitAt: Max.Players));

        builder.Append(" }");

        return builder.ToString();
    }

    public bool BitsEquals(in GameInput other) =>
        Mem.BytesEqual(AsReadOnlySpan(), other.AsReadOnlySpan());

    public bool Equals(in GameInput other, bool bitsOnly)
    {
        if (!bitsOnly && Frame != other.Frame)
            Tracer.Log("frames don't match: {}, {}", Frame, other.Frame);

        if (Size != other.Size)
            Tracer.Log("sizes don't match: {}, {}", Size, other.Size);

        if (buffer.Equals(other.buffer))
            Tracer.Log("bits don't match");

        Tracer.Assert(Size > 0 && other.Size > 0);

        return (bitsOnly || Frame == other.Frame)
               && Size == other.Size
               && BitsEquals(other);
    }

    // ReSharper disable once NonReadonlyMemberInGetHashCode
    public override int GetHashCode() => HashCode.Combine(Size, buffer, Frame);
    public bool Equals(GameInput other) => Equals(other, false);
    public override bool Equals(object? obj) => obj is GameInput gi && Equals(gi);
    public static bool operator ==(GameInput a, GameInput b) => a.Equals(b);
    public static bool operator !=(GameInput a, GameInput b) => !(a == b);
}