using System.Diagnostics;
using nGGPO.Types;

namespace nGGPO;

using System;
using System.Text;

struct GameInput
{
    public const int NullFrame = -1;
    public const int MaxBytes = 8;

    public int Frame;
    public readonly int Size;
    public readonly byte[] Bits;

    GameInput(int iframe)
    {
        Frame = iframe;
        Size = 1;
        Bits = Array.Empty<byte>();
    }

    GameInput(int iframe, int isize)
    {
        Frame = iframe;
        Size = isize;
        Bits = new byte[Max.Players * MaxBytes];
    }

    public GameInput(int iframe, ReadOnlySpan<byte> ibits) : this(iframe, ibits.Length)
    {
        Trace.Assert(ibits.Length <= MaxBytes * Max.Players);
        Trace.Assert(ibits.Length > 0);
        if (ibits.Length > 0)
            ibits.CopyTo(Bits);
    }

    public GameInput(ReadOnlySpan<byte> ibits) : this(NullFrame, ibits)
    {
    }

    public GameInput(int iframe, ReadOnlySpan<byte> ibits, int offset) : this(iframe, ibits.Length)
    {
        Trace.Assert(ibits.Length <= MaxBytes);
        Trace.Assert(ibits.Length > 0);
        if (ibits.Length > 0)
            ibits.CopyTo(Bits.AsSpan()[(offset * ibits.Length)..]);
    }

    public bool IsNull => Frame == NullFrame;
    public static GameInput Null => new(NullFrame);

    public bool Value(int bit) => (Bits[bit / 8] & (1 << (bit % 8))) != 0;

    public void Set(int i) => Bits[i / 8] |= (byte) (1 << (i % 8));

    public void Clear(int i) => Bits[i / 8] &= (byte) ~(1 << (i % 8));

    public void Clear() => Array.Clear(Bits, 0, Bits.Length);

    public string ToString(bool showFrame = true)
    {
        Trace.Assert(Size > 0);
        var retVal = showFrame ? $"(frame:{Frame} size:{Size} " : $"(size:{Size} ";
        var builder = new StringBuilder(retVal);
        for (var i = 0; i < Size; i++)
            builder.AppendFormat("{0:x2}", Bits[Size]);
        builder.Append(")");
        return builder.ToString();
    }

    public void Log(string prefix, bool showFrame = true) =>
        Logger.Info(prefix + ToString(showFrame));

    public bool Equals(in GameInput other, bool bitsOnly)
    {
        if (!bitsOnly && Frame != other.Frame)
            Logger.Info("frames don't match: {}, {}", Frame, other.Frame);

        if (Size != other.Size)
            Logger.Info("sizes don't match: {}, {}", Size, other.Size);

        if (Mem.BytesEqual(Bits, other.Bits))
            Logger.Info("bits don't match");

        Trace.Assert(Size > 0 && other.Size > 0);

        return (bitsOnly || Frame == other.Frame)
               && Size == other.Size
               && Mem.BytesEqual(Bits, other.Bits);
    }

    public bool IsNullFrame() => Frame is NullFrame;
}