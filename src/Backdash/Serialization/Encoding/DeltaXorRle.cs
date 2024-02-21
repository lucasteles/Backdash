using System.Diagnostics;
using Backdash.Core;
using Backdash.Data;

namespace Backdash.Serialization.Encoding;

static class DeltaXorRle
{
    [DebuggerDisplay("{ToString()}")]
    public ref struct Encoder
    {
        BitOffsetWriter bitWriter;
        readonly Span<byte> last;
        public int Count { get; private set; }

        public Encoder(Span<byte> output, Span<byte> lastBuffer)
        {
            Count = 0;
            last = lastBuffer;

            Trace.Assert(output.Length > 0);
            bitWriter = new(output);
        }

        public readonly ushort BitOffset => bitWriter.Offset;

        public bool Write(ReadOnlySpan<byte> current)
        {
            Trace.Assert(current.Length <= last.Length);

            if (!Mem.EqualBytes(current, last, truncate: true))
            {
                var currentBits = ReadOnlyBitVector.FromSpan(current);
                var lastBits = ReadOnlyBitVector.FromSpan(last);
                var lastOffset = bitWriter.Offset;
                try
                {
                    for (var i = 0; i < currentBits.BitCount; i++)
                    {
                        if (currentBits[i] == lastBits[i])
                            continue;

                        bitWriter.SetNext();

                        if (currentBits[i])
                            bitWriter.SetNext();
                        else
                            bitWriter.ClearNext();

                        bitWriter.WriteNibble(i);
                    }
                }
                catch (IndexOutOfRangeException)
                {
                    bitWriter.Offset = lastOffset;
                    return false;
                }
            }

            bitWriter.ClearNext();
            current.CopyTo(last);
            Count++;
            return true;
        }

        public override readonly string ToString() =>
            $"{{ Count: {Count}, Writer: {bitWriter.ToString()} }}";
    }

    [DebuggerDisplay("{ToString()}")]
    public ref struct Decoder
    {
        readonly ushort bitCount;
        BitOffsetWriter bitVector;

        public Decoder(Span<byte> buffer, ushort bitCount)
        {
            this.bitCount = bitCount;
            bitVector = new(buffer);
        }

        public bool Skip(int count)
        {
            if (count <= 0) return true;

            var lastOffset = bitVector.Offset;

            try
            {
                while (bitVector.Offset < bitCount)
                {
                    var skipNext = count-- > 0;
                    if (skipNext)
                    {
                        while (bitVector.Read())
                        {
                            bitVector.Read();
                            bitVector.ReadNibble();
                        }

                        continue;
                    }

                    Trace.Assert(bitVector.Offset <= bitCount);
                    return true;
                }
            }
            catch (IndexOutOfRangeException)
            {
                bitVector.Offset = lastOffset;
                return false;
            }

            return false;
        }

        public bool Read(in Span<byte> output)
        {
            var outputBits = BitVector.FromSpan(output);

            if (bitVector.Offset >= bitCount || bitVector.Completed)
                return false;

            var lastOffset = bitVector.Offset;
            try
            {
                while (bitVector.Read())
                {
                    var on = bitVector.Read();
                    var button = bitVector.ReadNibble();
                    if (on)
                        outputBits.Set(button);
                    else
                        outputBits.Clear(button);
                }
            }
            catch (IndexOutOfRangeException)
            {
                bitVector.Offset = lastOffset;
                return false;
            }

            Trace.Assert(bitVector.Offset <= bitCount);
            return true;
        }

        public override readonly string ToString() =>
            $"{{ Offset: {bitVector.Offset}/{bitCount}, " +
            $"Read: {Mem.GetBitString(bitVector.Buffer[..bitVector.Offset])}," +
            $"Pending: {Mem.GetBitString(bitVector.Buffer[bitVector.Offset..])}," +
            $"}}";
    }
}
