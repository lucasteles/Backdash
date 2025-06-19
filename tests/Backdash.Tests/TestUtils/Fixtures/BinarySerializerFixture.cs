using System.Buffers;
using Backdash.Core;
using Backdash.Network;
using Backdash.Serialization;

namespace Backdash.Tests.TestUtils.Fixtures;

readonly ref struct BinarySerializerFixture
{
    readonly byte[] buffer;
    public readonly BinaryBufferReader Reader;
    public readonly BinarySpanWriter Writer;
    public readonly ref int WriteOffset;
    public readonly ref int ReadOffset;

    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    readonly Offset offset = new();

    public BinarySerializerFixture(Endianness? endianness = null)
    {
        ReadOffset = ref offset.Read;
        WriteOffset = ref offset.Write;
        buffer = ArrayPool<byte>.Shared.Rent(Max.UdpPacketSize);
        Reader = new(buffer, ref ReadOffset, endianness);
        Writer = new(buffer, ref WriteOffset, endianness);
    }

    public void Dispose() => ArrayPool<byte>.Shared.Return(buffer, true);

    class Offset(int write = 0, int read = 0)
    {
        public int Write = write;
        public int Read = read;
    }
}
