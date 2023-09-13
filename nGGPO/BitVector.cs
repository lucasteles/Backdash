using System;
using System.Diagnostics;

namespace nGGPO;

public class BitVector
{
    public const int NibbleSize = 8;

    public static void SetBit(in Span<byte> vector, ref int offset)
    {
        vector[offset / 8] |= (byte) (1 << (offset % 8));
        offset += 1;
    }

    public static void ClearBit(in Span<byte> vector, ref int offset)
    {
        vector[offset / 8] &= (byte) ~(1 << (offset % 8));
        offset += 1;
    }

    public static void WriteNibblet(in Span<byte> vector, int nibble, ref int offset)
    {
        Trace.Assert(nibble < 1 << NibbleSize);
        for (var i = 0; i < NibbleSize; i++)
            if ((nibble & (1 << i)) != 0)
                SetBit(vector, ref offset);
            else
                ClearBit(vector, ref offset);
    }

    public static bool ReadBit(in Span<byte> vector, ref int offset)
    {
        var ret = (vector[offset / 8] & (1 << (offset % 8))) != 0;
        offset += 1;
        return ret;
    }

    public static int ReadNibblet(in Span<byte> vector, ref int offset)
    {
        var nibblet = 0;
        for (var i = 0; i < NibbleSize; i++)
            nibblet |= (ReadBit(vector, ref offset) ? 1 : 0) << i;

        return nibblet;
    }
}