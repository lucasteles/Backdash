using Backdash.Network.Messages;
using Backdash.Serialization.Encoding;

namespace Backdash.Network.Protocol.Comm;

static class InputEncoder
{
    public static DeltaXorRle.Encoder GetCompressor(in InputMessage inputMsg, Span<byte> lastBuffer) =>
        new(inputMsg.Bits.Span, lastBuffer);

    public static DeltaXorRle.Decoder GetDecompressor(in InputMessage inputMsg) =>
        new(inputMsg.Bits.Span, inputMsg.NumBits);
}
