using System.Numerics;
using Backdash.Core;
using Backdash.Serialization.Encoding;

namespace Backdash.Tests.Specs.Unit.Serialization.Encoding;

public class DeltaXorTests
{
    [Fact]
    public void ShouldCompressAndDecompressSample()
    {
        Span<byte> baseValue = [1, 0];

        byte[][] valueList =
        [
            [2, 1],
            [4, 3],
            [6, 7],
        ];

        var encodedValue = Encode(baseValue, valueList);

        var decodedValues = Decode(baseValue, encodedValue);

        decodedValues.Should().BeEquivalentTo(valueList);

        decodedValues
            .Select(x => Mem.GetBitString(x))
            .Should().BeEquivalentTo(
                "00000010-00000001",
                "00000100-00000011",
                "00000110-00000111"
            );
    }


    [Fact]
    public void ShouldCompressAndDecompressSampleUsingSimd()
    {
        var size = Vector<byte>.Count + 1;

        Span<byte> baseValue = Enumerable.Repeat((byte)0, size).ToArray();

        byte[][] valueList =
        [
            Enumerable.Repeat((byte)2, size).ToArray(),
            Enumerable.Repeat((byte)4, size).ToArray(),
        ];

        var encodedValue = Encode(baseValue, valueList);
        var decodedValues = Decode(baseValue, encodedValue);
        decodedValues.Should().BeEquivalentTo(valueList);
    }

    static byte[] Encode(Span<byte> baseValue, byte[][] valueList)
    {
        var result = new byte[valueList.Sum(x => x.Length)];
        var lastBuffer = baseValue.ToArray();
        DeltaXor.Encoder encoder = new(result, lastBuffer);
        foreach (var value in valueList)
            encoder.Write(value);
        return result;
    }

    static IReadOnlyList<byte[]> Decode(Span<byte> baseValue, byte[] encodedValues)
    {
        var lastBuffer = baseValue.ToArray();
        List<byte[]> result = [];

        var decodeBuffer = new byte[baseValue.Length];
        DeltaXor.Decoder encoder = new(encodedValues, lastBuffer);

        while (encoder.Read(decodeBuffer))
            result.Add([.. decodeBuffer]);

        return result;
    }
}
