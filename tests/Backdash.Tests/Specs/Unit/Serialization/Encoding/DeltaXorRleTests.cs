using Backdash.Data;
using Backdash.Serialization.Encoding;
namespace Backdash.Tests.Specs.Unit.Serialization.Encoding;
public class DeltaXorRleTests
{
    [Fact]
    public void ShouldCompressedBeLessThenUncompressed()
    {
        byte[] baseValue = [0, 0, 0, 1];
        byte[][] valueList =
        [
            [0, 2, 0, 0],
            [1, 0, 0, 4],
            [0, 0, 0, 8],
        ];
        var (_, bitCount) = Encode(baseValue, valueList);
        var originalLen = valueList.Sum(x => x.Length);
        var compressedLen = bitCount / (double)ByteSize.ByteToBits;
        var byteGain = originalLen - compressedLen;
        byteGain.Should().BeGreaterThanOrEqualTo(0);
    }
    [Fact]
    public void ShouldCompressAndDecompressSample()
    {
        byte[] baseValue = [1, 0, 0];
        byte[][] valueList =
        [
            [0, 2, 0],
            [2, 0, 4],
            [4, 0, 8],
        ];
        var (encodedValue, bitCount) = Encode(baseValue, valueList);
        var decodedValues = Decode(encodedValue, baseValue, bitCount);
        decodedValues.Should().BeEquivalentTo(valueList);
    }
    static (byte[], ushort) Encode(ReadOnlySpan<byte> baseValue, byte[][] valueList)
    {
        var result = new byte[128];
        var lastBuffer = baseValue.ToArray();
        DeltaXorRle.Encoder encoder = new(result, lastBuffer);
        foreach (var value in valueList)
            encoder.Write(value).Should().BeTrue();
        return (result, encoder.BitOffset);
    }
    static IReadOnlyList<byte[]> Decode(byte[] encodedValues, ReadOnlySpan<byte> baseValue, ushort numBits)
    {
        List<byte[]> result = [];
        DeltaXorRle.Decoder encoder = new(encodedValues, numBits);
        var lastBuffer = baseValue.ToArray();
        while (encoder.Read(lastBuffer))
            result.Add([.. lastBuffer]);
        return result;
    }
}
