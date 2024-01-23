using nGGPO.Utils;

namespace nGGPO.Data;

readonly ref struct BitVector(scoped ref Span<byte> bits)
{
    public int Size => Buffer.Length;
    public int BitCount => Size * Mem.ByteSize;
    public Span<byte> Buffer { get; } = bits;

    public static void SetBit(in Span<byte> vector, int index) =>
        vector[index / 8] |= (byte)(1 << (index % 8));

    public static bool GetBit(in Span<byte> vector, int index) =>
        (vector[index / 8] & (1 << (index % 8))) != 0;

    public static void ClearBit(in Span<byte> vector, int index) =>
        vector[index / 8] &= (byte)~(1 << (index % 8));

    public bool Get(int i) => GetBit(Buffer, i);
    public void Set(int i) => SetBit(Buffer, i);
    public void Clear(int i) => ClearBit(Buffer, i);

    public void Erase() => Buffer.Clear();

    public bool this[int bit]
    {
        get => Get(bit);
        set
        {
            if (value) Set(bit);
            else Clear(bit);
        }
    }

    public static implicit operator Span<byte>(BitVector @this) => @this.Buffer;

    public ref struct BitOffset(ref Span<byte> buffer, int offset = 0)
    {
        public const int NibbleSize = 8;

        readonly Span<byte> bytes = buffer;

        public int Offset { get; private set; } = offset;

        public void Inc() => Offset++;

        public void SetNext()
        {
            SetBit(bytes, Offset);
            Inc();
        }

        public bool Read()
        {
            var ret = GetBit(bytes, Offset);
            Inc();
            return ret;
        }

        public void ClearNext()
        {
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

    public bool IsEmpty => Buffer.Length is 0;
}
