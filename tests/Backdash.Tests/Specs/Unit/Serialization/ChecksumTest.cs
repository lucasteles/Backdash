using Backdash.Synchronizing.State;
using Backdash.Tests.TestUtils;

namespace Backdash.Tests.Specs.Unit.Serialization;

public class CheckSumTests
{
    static readonly Fletcher32ChecksumProvider fletcher32ChecksumProvider = new();

    [PropertyTest(MaxTest = 10_000)]
    public bool ShouldCalculateOddBytesPaddingZero(OddSizeArray<byte> oddBytes)
    {
        byte[] evenBytes = [.. oddBytes.Values, 0];

        var oddHash = fletcher32ChecksumProvider.Compute(oddBytes.Values);
        var evenHash = fletcher32ChecksumProvider.Compute(evenBytes);

        return evenHash == oddHash;
    }

    [Fact]
    public void TestOddByteArray()
    {
        byte[] oddBytes = [1];
        byte[] evenBytes = [.. oddBytes, 0];

        var oddHash = fletcher32ChecksumProvider.Compute(oddBytes);
        var evenHash = fletcher32ChecksumProvider.Compute(evenBytes);

        Assert.Equal(evenHash, oddHash);
    }
}
