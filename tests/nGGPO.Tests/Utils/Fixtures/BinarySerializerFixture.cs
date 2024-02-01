using System.Buffers;
using nGGPO.Serialization.Buffer;
using nGGPO.Utils;

namespace nGGPO.Tests.Utils.Fixtures;

readonly ref struct BinarySerializerFixture
{
    public readonly byte[] Buffer;
    public readonly NetworkBufferReader Reader;
    public readonly NetworkBufferWriter Writer;

    public readonly ref int WriteOffset;
    public readonly ref int ReadOffset;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    readonly Offset offset = new();

    public Span<byte> Span => Buffer;

    public BinarySerializerFixture()
    {
        ReadOffset = ref offset.Read;
        WriteOffset = ref offset.Write;
        Buffer = ArrayPool<byte>.Shared.Rent(Max.UdpPacketSize);
        Reader = new(Buffer, ref ReadOffset);
        Writer = new(Buffer, ref WriteOffset);
    }

    public void Dispose() => ArrayPool<byte>.Shared.Return(Buffer, true);

    class Offset(int write = 0, int read = 0)
    {
        public int Write = write;
        public int Read = read;
    }
}
