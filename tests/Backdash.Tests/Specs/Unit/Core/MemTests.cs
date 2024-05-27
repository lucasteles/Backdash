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
}
