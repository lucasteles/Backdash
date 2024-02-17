using System.Buffers;
using Backdash.Core;
using Backdash.Serialization.Buffer;

namespace Backdash.Tests.Utils.Fixtures;

readonly ref struct BinarySerializerFixture
{
    public readonly byte[] Buffer;
    public readonly BinaryBufferReader Reader;
    public readonly BinaryBufferWriter Writer;

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
