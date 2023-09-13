using System;
using System.Diagnostics;
using nGGPO.Types;

namespace nGGPO;

using System.Text;

struct GameInput : IEquatable<GameInput>
{
    public const int NullFrame = -1;
    public const int MaxBytes = 8;

    public int Frame { get; set; } = NullFrame;
    public int Size { get; }

    public BitVector Bits { get; }

    public static GameInput Empty => new(size: 1);

    public GameInput(int size)
    {
        Size = size;
        Bits = BitVector.Empty;
    }

    public GameInput(byte[] ibits, int size)
    {
        Trace.Assert(ibits.Length <= MaxBytes * Max.Players);
        Trace.Assert(ibits.Length > 0);

        Size = size;
        Bits = new(ibits);
    }

    public GameInput(byte[] ibits) : this(ibits, ibits.Length)
    {
    }

    public bool IsNullFrame => Frame is NullFrame;

    public void Clear() => Bits.Erase();

    public override string ToString()
    {
        var builder = new StringBuilder();

        builder.AppendFormat("{{ Frame: {0},", Frame);
        builder.AppendFormat(" Size: {0}, Input: ", Size);

        builder.Append(Bits.ToString(splitAt: Max.Players));

        builder.Append(" }");

        return builder.ToString();
    }

    public bool Equals(GameInput other, bool bitsOnly)
    {
        if (!bitsOnly && Frame != other.Frame)
            Logger.Info("frames don't match: {}, {}", Frame, other.Frame);

        if (Size != other.Size)
            Logger.Info("sizes don't match: {}, {}", Size, other.Size);

        if (Bits.Equals(other.Bits))
            Logger.Info("bits don't match");

        Trace.Assert(Size > 0 && other.Size > 0);

        return (bitsOnly || Frame == other.Frame)
               && Size == other.Size
               && Bits.Equals(other.Bits);
    }

    // ReSharper disable once NonReadonlyMemberInGetHashCode
    public override int GetHashCode() => HashCode.Combine(Size, Bits, Frame);
    public bool Equals(GameInput other) => Equals(other, false);
    public override bool Equals(object? obj) => obj is GameInput gi && Equals(gi);
    public static bool operator ==(GameInput a, GameInput b) => a.Equals(b);
    public static bool operator !=(GameInput a, GameInput b) => !(a == b);
}