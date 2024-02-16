using Backdash.Serialization.Encoding;

namespace Backdash.Tests.Specs.Unit.Serialization.Encoding;

public class RunLengthEncodingTests
{
    [Fact]
    public void ShouldCompressAndDecompressSample()
    {
        byte[] values = [1, 1, 2, 2, 3, 3, 3];

        var encodeBuffer = new byte[values.Length * 2].AsSpan();
        RunLengthEncoding.Encode(values, encodeBuffer, out var totalWritten).Should().BeTrue();

        var decodeBuffer = new byte[values.Length * 2].AsSpan();
        RunLengthEncoding.Decode(encodeBuffer[..totalWritten], decodeBuffer, out var totalRead).Should().BeTrue();

        var decodedValues = decodeBuffer[..totalRead].ToArray();

        decodedValues.Should().BeEquivalentTo(values);
    }
}
