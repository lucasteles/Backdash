using System.Buffers;
using Backdash.Core;
using Backdash.Serialization.Buffer;

namespace Backdash.Tests.TestUtils.Fixtures;

readonly ref struct BinarySerializerFixture
{
    readonly byte[] buffer;
    public readonly BinaryBufferReader Reader;
    public readonly BinaryRawBufferWriter Writer;
    public readonly ref int WriteOffset;
    public readonly ref int ReadOffset;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    readonly Offset offset = new();

    public BinarySerializerFixture()
    {
        ReadOffset = ref offset.Read;
        WriteOffset = ref offset.Write;
        buffer = ArrayPool<byte>.Shared.Rent(Max.UdpPacketSize);
        Reader = new(buffer, ref ReadOffset);
        Writer = new(buffer, ref WriteOffset);
    }

    public void Dispose() => ArrayPool<byte>.Shared.Return(buffer, true);

    class Offset(int write = 0, int read = 0)
    {
        public int Write = write;
        public int Read = read;
    }
}
