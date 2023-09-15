using System;
using System.Diagnostics;
using nGGPO.Utils;

namespace nGGPO.DataStructure;

struct BitVector : IEquatable<BitVector>
{
    public static BitVector Empty = new(Array.Empty<byte>());

    public byte[] Bits { get; }
    public int Size => Bits.Length;
    public int BitCount => Size * Mem.ByteSize;

    public BitVector(in byte[] bits) => Bits = bits;

    public static void SetBit(in Span<byte> vector, int index) =>
        vector[index / 8] |= (byte) (1 << (index % 8));

    public static bool GetBit(in Span<byte> vector, int index) =>
        (vector[index / 8] & (1 << (index % 8))) != 0;

    public static void ClearBit(in Span<byte> vector, int index) =>
        vector[index / 8] &= (byte) ~(1 << (index % 8));

    public bool Get(int i) => GetBit(Bits, i);
    public void Set(int i) => SetBit(Bits, i);
    public void Clear(int i) => ClearBit(Bits, i);

    public void Erase() => Array.Clear(Bits, 0, Bits.Length);

    public bool this[int bit]
    {
        get => Get(bit);
        set
        {
            if (value) Set(bit);
            else Clear(bit);
        }
    }

    public override string ToString() => ToString(splitAt: 0);

    public string ToString(int splitAt, int bytePad = Mem.ByteSize) =>
        Mem.GetBitString(Bits, splitAt, bytePad);

    public bool Equals(BitVector other) => Mem.BytesEqual(Bits, other.Bits);
    public override bool Equals(object? obj) => obj is BitVector v && Equals(v);
    public override int GetHashCode() => Bits.GetHashCode();
    public static bool operator ==(BitVector a, BitVector b) => a.Equals(b);
    public static bool operator !=(BitVector a, BitVector b) => !(a == b);
    public static explicit operator byte[](BitVector @this) => @this.Bits;

    public ref struct BitOffsetWriter
    {
        public const int NibbleSize = 4;

        readonly Span<byte> vector;

        public int Offset { get; private set; }

        public BitOffsetWriter(in Span<byte> vector, int offset = 0)
        {
            this.vector = vector;
            Offset = offset;
        }

        public void Inc() => Offset++;

        public void SetNext()
        {
            SetBit(in vector, Offset);
            Inc();
        }

        public bool Read()
        {
            var ret = GetBit(in vector, Offset);
            Inc();
            return ret;
        }

        public void ClearNext()
        {
            ClearBit(vector, Offset);
            Inc();
        }

        public void WriteNibble(int nibble)
        {
            Trace.Assert(nibble < 1 << NibbleSize);
            for (var i = 0; i < NibbleSize; i++)
                if ((nibble & (1 << i)) != 0)
                    SetNext();
                else
                    ClearNext();
        }

        public int ReadNibble()
        {
            var nibble = 0;
            for (var i = 0; i < NibbleSize; i++)
                nibble |= (Read() ? 1 : 0) << i;

            return nibble;
        }
    }

    public bool IsEmpty => Bits.Length is 0;
}