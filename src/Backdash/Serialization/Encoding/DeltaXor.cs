using System.Diagnostics;
using Backdash.Core;

namespace Backdash.Serialization.Encoding;

public static class DeltaXor
{
    public ref struct Decoder
    {
        readonly Span<byte> messageBuffer;
        readonly Span<byte> lastBuffer;
        public int Count { get; private set; }
        public int ByteOffset { get; private set; }

        public Decoder(Span<byte> messageBuffer, Span<byte> lastBuffer)
        {
            this.messageBuffer = messageBuffer;
            this.lastBuffer = lastBuffer;

            Trace.Assert(this.lastBuffer.Length > 0);
            Trace.Assert(this.messageBuffer.Length > this.lastBuffer.Length);
        }

        public bool Read(Span<byte> next)
        {
            var nextBuffer = messageBuffer[ByteOffset..];

            if (nextBuffer.IsEmpty)
                return false;

            Trace.Assert(lastBuffer.Length >= next.Length);
            ByteOffset += Mem.Xor(lastBuffer[..next.Length], nextBuffer[..next.Length], next);
            next.CopyTo(lastBuffer);
            Count++;
            return true;
        }
    }

    [DebuggerDisplay("{ToString()}")]
    public ref struct Encoder
    {
        readonly Span<byte> messageBuffer;
        readonly Span<byte> lastBuffer;

        public int Count { get; private set; }
        public int ByteOffset { get; private set; }

        public Encoder(Span<byte> messageBuffer, Span<byte> lastBuffer)
        {
            this.lastBuffer = lastBuffer;
            this.messageBuffer = messageBuffer;
            Trace.Assert(this.lastBuffer.Length > 0);
            Trace.Assert(this.messageBuffer.Length >= this.lastBuffer.Length);
        }

        public bool Write(ReadOnlySpan<byte> next)
        {
            var nextBuffer = messageBuffer[ByteOffset..];

            if (next.Length > nextBuffer.Length)
                return false;

            Trace.Assert(lastBuffer.Length >= next.Length);
            ByteOffset += Mem.Xor(lastBuffer[..next.Length], next, nextBuffer);
            next.CopyTo(lastBuffer);
            Count++;
            return true;
        }

        public override readonly string ToString() => $"{{ Count: {Count} }}";
    }
}
