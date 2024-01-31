using System.Diagnostics;
using System.Runtime.CompilerServices;
using nGGPO.Utils;

namespace nGGPO.Data;

readonly ref struct ReadOnlyBitVector(ReadOnlySpan<byte> bits)
{
    public readonly ReadOnlySpan<byte> Buffer = bits;

    public int Size => Buffer.Length;
    public int BitCount => Size * Mem.ByteSize;

    public bool Get(int i) => BitVector.GetBit(Buffer, i);

    public bool this[int bit] => Get(bit);
}

readonly ref struct BitVector(Span<byte> bits)
{
    public readonly Span<byte> Buffer = bits;

    public int Size => Buffer.Length;
    public int BitCount => Size * Mem.ByteSize;

    public static void SetBit(in Span<byte> vector, int index) =>
        vector[index / 8] |= (byte)(1 << (index % 8));

    public static bool GetBit(in ReadOnlySpan<byte> vector, int index) =>
        (vector[index / 8] & (1 << (index % 8))) != 0;

    public static void ClearBit(in Span<byte> vector, int index) =>
        vector[index / 8] &= (byte)~(1 << (index % 8));

    public bool Get(int i) => GetBit(Buffer, i);
    public void Set(int i) => SetBit(Buffer, i);
    public void Clear(int i) => ClearBit(Buffer, i);

    public bool this[int bit]
    {
        get => Get(bit);
        set
        {
            if (value) Set(bit);
            else Clear(bit);
        }
    }

    public static implicit operator ReadOnlyBitVector(BitVector @this) => new(@this.Buffer);

    [DebuggerDisplay("{ToString()}")]
    public ref struct BitOffset(Span<byte> buffer, ushort offset = 0)
    {
        public const int NibbleSize = 8;

        readonly Span<byte> bytes = buffer;

        public ushort Offset { get; private set; } = offset;

        public void Inc() => Offset++;

        public override readonly string ToString()
        {
            var byteOffset = Offset / Mem.ByteSize;
            return
                $"{{TotalWrite: {byteOffset}, Offset: {Offset}}} [{(Offset is 0 ? "" : Mem.GetBitString(bytes[..byteOffset]))}]";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        readonly void CheckOffset()
        {
            if (Offset >= bytes.Length * NibbleSize)
                throw new NggpoException($"BitOffset index overflow: {Offset} (Buffer: {bytes.Length * NibbleSize})");
        }

        public void SetNext()
        {
            CheckOffset();
            SetBit(bytes, Offset);
            Inc();
        }

        public bool Read()
        {
            CheckOffset();
            var ret = GetBit(bytes, Offset);
            Inc();
            return ret;
        }

        public void ClearNext()
        {
            CheckOffset();
            ClearBit(bytes, Offset);
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
}
