using System;
using nGGPO.Utils;

namespace nGGPO.DataStructure;

struct BitVector : IEquatable<BitVector>
{
    public static BitVector Empty = new(Memory<byte>.Empty);
    public int Size => Memory.Length;
    public int BitCount => Size * Mem.ByteSize;

    public Memory<byte> Memory { get; }
    public BitVector(in Memory<byte> bits) => Memory = bits;

    public static void SetBit(in Span<byte> vector, int index) =>
        vector[index / 8] |= (byte) (1 << (index % 8));

    public static bool GetBit(in Span<byte> vector, int index) =>
        (vector[index / 8] & (1 << (index % 8))) != 0;

    public static void ClearBit(in Span<byte> vector, int index) =>
        vector[index / 8] &= (byte) ~(1 << (index % 8));

    public bool Get(int i) => GetBit(Memory.Span, i);
    public void Set(int i) => SetBit(Memory.Span, i);
    public void Clear(int i) => ClearBit(Memory.Span, i);

    public void Erase() => Memory.Span.Clear();

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
        Mem.GetBitString(Memory.Span, splitAt, bytePad);

    public bool Equals(BitVector other) => Mem.BytesEqual(Memory.Span, other.Memory.Span);
    public override bool Equals(object? obj) => obj is BitVector v && Equals(v);
    public override int GetHashCode() => Memory.Span.GetHashCode();
    public static bool operator ==(BitVector a, BitVector b) => a.Equals(b);
    public static bool operator !=(BitVector a, BitVector b) => !(a == b);
    public static implicit operator Memory<byte>(BitVector @this) => @this.Memory;

    public struct BitOffset
    {
        public const int NibbleSize = 4;

        readonly Memory<byte> bytes;

        public int Offset { get; private set; }

        public BitOffset(Memory<byte> bytes, int offset = 0)
        {
            this.bytes = bytes;
            Offset = offset;
        }

        public void Inc() => Offset++;

        public void SetNext()
        {
            SetBit(bytes.Span, Offset);
            Inc();
        }

        public bool Read()
        {
            var ret = GetBit(bytes.Span, Offset);
            Inc();
            return ret;
        }

        public void ClearNext()
        {
            ClearBit(bytes.Span, Offset);
            Inc();
        }

        public void WriteNibble(int nibble)
        {
            Tracer.Assert(nibble < 1 << NibbleSize);
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

    public bool IsEmpty => Memory.Span.Length is 0;
}