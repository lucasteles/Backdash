using Backdash.Network.Messages;
using Backdash.Serialization.Encoding;

namespace Backdash.Network.Protocol.Comm;

static class InputEncoder
{
    public static DeltaXorRle.Encoder GetCompressor(ref InputMessage inputMsg, Span<byte> lastBuffer) =>
        new(inputMsg.Bits, lastBuffer);

    public static DeltaXorRle.Decoder GetDecompressor(ref InputMessage inputMsg) =>
        new(inputMsg.Bits, inputMsg.NumBits);
}
