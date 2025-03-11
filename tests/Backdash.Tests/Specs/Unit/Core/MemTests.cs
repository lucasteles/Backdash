using System.Buffers.Binary;
using System.Net;
using System.Numerics;
using Backdash.Core;
using Backdash.Tests.TestUtils;

namespace Backdash.Tests.Specs.Unit.Core;

public class MemTests
{
    [PropertyTest]
    public bool ShouldHaveSamePopCountForAnyEndiannessInt(int[] values)
    {
        var reversed = Array.ConvertAll(values, IPAddress.HostToNetworkOrder);
        return Mem.PopCount(values) == Mem.PopCount(reversed);
    }

    [PropertyTest]
    public bool ShouldHaveSamePopCountForAnyEndiannessLong(long[] values)
    {
        var reversed = Array.ConvertAll(values, IPAddress.HostToNetworkOrder);
        return Mem.PopCount(values) == Mem.PopCount(reversed);
    }

    [PropertyTest]
    public bool ShouldHaveSamePopCountForAnyEndiannessInt128(Int128[] values)
    {
        var reversed = Array.ConvertAll(values, host =>
            BitConverter.IsLittleEndian ? BinaryPrimitives.ReverseEndianness(host) : host
        );
        return Mem.PopCount(values) == Mem.PopCount(reversed);
    }

    [PropertyTest]
    public bool ShouldHaveSamePopCountForAnyEndiannessVector2(Vector2[] values)
    {
        var reversed = Array.ConvertAll(values, host =>
            BitConverter.IsLittleEndian ? new(ReverseFloat(host.X), ReverseFloat(host.Y)) : host
        );

        return Mem.PopCount(values) == Mem.PopCount(reversed);

        static float ReverseFloat(float host) =>
            BitConverter.Int32BitsToSingle(BinaryPrimitives.ReverseEndianness(BitConverter.SingleToInt32Bits(host)));
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(1, 0, 1)]
    [InlineData(2, 0, 1)]
    [InlineData(3, 0, 2)]
    [InlineData(1, 2, 2)]
    [InlineData(3, 1, 3)]
    [InlineData(3, 3, 4)]
    public void TestPopCount(int value1, int value2, int expectedCount)
    {
        ReadOnlySpan<int> span = [value1, value2];
        var count = Mem.PopCount(span);
        count.Should().Be(expectedCount);
    }
}
