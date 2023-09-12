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
    public int Size;
    public byte[] Bits;

    GameInput(int iframe, int isize)
    {
        Trace.Assert(isize > 0);
        Frame = iframe;
        Size = isize;
        Bits = new byte[Max.Players * MaxBytes];
    }

    public GameInput(int iframe, ReadOnlySpan<byte> ibits) : this(iframe, ibits.Length)
    {
        Trace.Assert(ibits.Length <= MaxBytes * Max.Players);
        if (ibits.Length > 0)
            ibits.CopyTo(Bits);
    }

    public GameInput(ReadOnlySpan<byte> ibits) : this(NullFrame, ibits)
    {
    }

    public GameInput(int iframe, ReadOnlySpan<byte> ibits, int offset) : this(iframe, ibits.Length)
    {
        Trace.Assert(ibits.Length <= MaxBytes);
        if (ibits.Length > 0)
            ibits.CopyTo(Bits.AsSpan()[(offset * ibits.Length)..]);
    }

    public bool IsNull => Frame == NullFrame;

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

        if (ByteArraysEqual(Bits, other.Bits))
            Logger.Info("bits don't match");

        Trace.Assert(Size > 0 && other.Size > 0);

        return (bitsOnly || Frame == other.Frame)
               && Size == other.Size
               && ByteArraysEqual(Bits, other.Bits);
    }

    static bool ByteArraysEqual(ReadOnlySpan<byte> a1, ReadOnlySpan<byte> a2) =>
        a1.SequenceEqual(a2);
}