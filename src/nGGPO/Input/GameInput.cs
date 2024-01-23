using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Text;
using nGGPO.DataStructure;
using nGGPO.Utils;

namespace nGGPO.Input;

[InlineArray(Capacity)]
public struct GameInputBuffer
{
    byte element0;

    public const int Capacity = Max.InputBytes * Max.InputPlayers;

    public override readonly string ToString() =>
        Mem.GetBitString(this, splitAt: Max.InputBytes);

    public readonly bool Equals(GameInputBuffer other) =>
        Mem.SpanEqual<byte>(this, other, truncate: true);

    public GameInputBuffer(ReadOnlySpan<byte> bits) => bits.CopyTo(this);

    public static Span<byte> GetPlayer(ref GameInputBuffer buffer, int playerIndex)
    {
        var byteIndex = playerIndex * Max.InputBytes;
        return buffer[byteIndex..(byteIndex + Max.InputBytes)];
    }
}

struct GameInput : IEquatable<GameInput>
{
    public Frame Frame { get; private set; } = Frame.Null;

    public GameInputBuffer Buffer;

    public int Size { get; set; }

    public static GameInput Empty => new()
    {
        Frame = Frame.Null,
        Size = 0,
    };

    public GameInput()
    {
        Buffer = new();
        Size = GameInputBuffer.Capacity;
    }

    public GameInput(ref GameInputBuffer inputBuffer, int size)
    {
        ReadOnlySpan<byte> bits = inputBuffer;
        Tracer.Assert(bits.Length <= Max.InputBytes * Max.MsgPlayers);
        Tracer.Assert(bits.Length > 0);
        Size = size;
        Buffer = inputBuffer;
    }

    public GameInput(ReadOnlySpan<byte> bits)
    {
        Tracer.Assert(bits.Length <= Max.InputBytes * Max.MsgPlayers);
        Tracer.Assert(bits.Length > 0);
        Size = bits.Length;
        Buffer = new(bits);
    }

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Span<byte> AsSpan() => Mem.InlineArrayAsSpan<GameInputBuffer, byte>(ref Buffer, Size);

    [Pure]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly ReadOnlySpan<byte> AsReadOnlySpan() =>
        Mem.InlineArrayAsReadOnlySpan<GameInputBuffer, byte>(in Buffer, Size);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BitVector GetBitVector()
    {
        var span = AsSpan();
        return new(ref span);
    }

    public readonly bool IsEmpty => Size is 0;
    public void IncrementFrame() => Frame = Frame.Next;
    public void SetFrame(Frame frame) => Frame = frame;
    public void ResetFrame() => Frame = Frame.Null;
    public void Clear() => AsSpan().Clear();

    public override readonly string ToString()
    {
        StringBuilder builder = new();
        builder.Append($"{{ Frame: {Frame},");
        builder.Append($" Size: {Size}, Input: ");
        builder.Append(Buffer.ToString());
        builder.Append(" }");
        return builder.ToString();
    }

    public readonly bool Equals(in GameInput other, bool bitsOnly)
    {
        if (!bitsOnly && Frame != other.Frame)
            Tracer.Log("frames don't match: {}, {}", Frame, other.Frame);

        if (Size != other.Size)
            Tracer.Log("sizes don't match: {}, {}", Size, other.Size);

        var sameBits = Buffer.Equals(other.Buffer);

        if (sameBits)
            Tracer.Log("bits don't match");

        return (bitsOnly || Frame == other.Frame)
               && Size == other.Size
               && sameBits;
    }

    public readonly bool Equals(GameInput other) => Equals(other, false);
    public override readonly bool Equals(object? obj) => obj is GameInput gi && Equals(gi);

#pragma warning disable S2328
    public override readonly int GetHashCode() => HashCode.Combine(Size, Buffer, Frame);
#pragma warning restore S2328

    public static bool operator ==(GameInput a, GameInput b) => a.Equals(b);
    public static bool operator !=(GameInput a, GameInput b) => !(a == b);
}
