using System.Diagnostics;
using Backdash.Core;

namespace Backdash.Data;

[DebuggerDisplay("{ToString()}")]
readonly ref struct ReadOnlyBitVector(scoped in ReadOnlySpan<byte> bits)
{
    public readonly ReadOnlySpan<byte> Buffer = bits;
    public int Size => Buffer.Length;
    public int BitCount => Size * ByteSize.ByteToBits;
    public bool Get(int i) => BitVector.GetBit(in Buffer, i);
    public bool this[int bit] => Get(bit);
    public static ReadOnlyBitVector FromSpan(scoped in ReadOnlySpan<byte> bits) => new(bits);
    public override string ToString() => Mem.GetBitString(Buffer);
}

[DebuggerDisplay("{ToString()}")]
readonly ref struct BitVector(scoped in Span<byte> bits)
{
    public static BitVector FromSpan(scoped in Span<byte> bits) => new(bits);
    public readonly Span<byte> Buffer = bits;
    public int ByteLength => Buffer.Length;
    public int Length => ByteLength * ByteSize.ByteToBits;

    public static void SetBit(in Span<byte> vector, int index) =>
        vector[index / 8] |= (byte)(1 << (index % 8));

    public static bool GetBit(in ReadOnlySpan<byte> vector, int index) =>
        (vector[index / 8] & (1 << (index % 8))) != 0;

    public static void ClearBit(in Span<byte> vector, int index) =>
        vector[index / 8] &= (byte)~(1 << (index % 8));

    public bool Get(int i) => GetBit(Buffer, i);
    public void Set(int i) => SetBit(in Buffer, i);
    public void Clear(int i) => ClearBit(in Buffer, i);

    public bool this[int bit]
    {
        get => Get(bit);
        set
        {
            if (value) Set(bit);
            else Clear(bit);
        }
    }

    public override string ToString() => Mem.GetBitString(Buffer);
    public static implicit operator ReadOnlyBitVector(BitVector @this) => new(@this.Buffer);
}

[DebuggerDisplay("{ToString()}")]
ref struct BitOffsetWriter(Span<byte> buffer, ushort offset = 0, int nibbleSize = ByteSize.ByteToBits)
{
    public readonly Span<byte> Buffer = buffer;
    public ushort Offset { get; set; } = offset;
    public readonly int Capacity => Buffer.Length * ByteSize.ByteToBits;
    public readonly bool Completed => Offset >= Capacity;
    public void Inc() => Offset++;

    public override readonly string ToString() =>
        $"{{Offset: {Offset}/{Capacity}, Written: '{(Offset is 0 ? "" : Mem.GetBitString(Buffer[..(Offset * ByteSize.ByteToBits)]))}'}}";

    public void SetNext()
    {
        BitVector.SetBit(in Buffer, Offset);
        Inc();
    }

    public bool Read()
    {
        var ret = BitVector.GetBit(Buffer, Offset);
        Inc();
        return ret;
    }

    public void ClearNext()
    {
        BitVector.ClearBit(in Buffer, Offset);
        Inc();
    }

    public void WriteNibble(int nibble)
    {
        Trace.Assert(nibble < 1 << nibbleSize);
        for (var i = 0; i < nibbleSize; i++)
            if ((nibble & (1 << i)) != 0)
                SetNext();
            else
                ClearNext();
    }

    public int ReadNibble()
    {
        var nibble = 0;
        for (var i = 0; i < nibbleSize; i++)
            nibble |= (Read() ? 1 : 0) << i;
        return nibble;
    }
}
