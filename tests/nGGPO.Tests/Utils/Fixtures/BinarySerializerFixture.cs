using nGGPO.Network;
using nGGPO.Serialization.Buffer;
using nGGPO.Utils;

namespace nGGPO.Tests.Utils.Fixtures;

readonly ref struct BinarySerializerFixture
{
    public readonly MemoryBuffer<byte> Buffer;
    public readonly NetworkBufferReader Reader;
    public readonly NetworkBufferWriter Writer;

    public readonly ref int WriteOffset;
    public readonly ref int ReadOffset;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    readonly Offset offset = new();

    public Span<byte> Span => Buffer.Span;

    public BinarySerializerFixture()
    {
        ReadOffset = ref offset.Read;
        WriteOffset = ref offset.Write;
        Buffer = MemoryBuffer.Rent(UdpPeerClient.UdpPacketSize, true);
        Reader = new(Buffer, ref ReadOffset);
        Writer = new(Buffer, ref WriteOffset);
    }

    public readonly void Dispose() => Buffer.Dispose();

    class Offset(int write = 0, int read = 0)
    {
        public int Write = write;
        public int Read = read;
    }
}
