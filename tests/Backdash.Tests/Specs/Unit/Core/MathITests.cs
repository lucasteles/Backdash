using Backdash.Tests.TestUtils;

namespace Backdash.Tests.Specs.Unit.Core;

public class MathITests
{
    [PropertyTest]
    public bool ShouldSumArrayOfInt(int[] values)
    {
        var sut = MathI.Sum(values);
        var native = values.Sum();
        return sut == native;
    }

    [PropertyTest]
    public bool ShouldSumArrayOfUInt(uint[] values)
    {
        var sut = MathI.Sum(values);
        var native = values.Aggregate<uint, uint>(0, (current, value) => current + value);
        return sut == native;
    }

    [PropertyTest]
    public bool ShouldRawSumArrayOfInt(int[] values)
    {
        var sut = MathI.SumRaw(values);
        var native = values.Sum();
        return sut == native;
    }

    [PropertyTest]
    public bool ShouldRawSumArrayOfUInt(uint[] values)
    {
        var sut = MathI.SumRaw(values);
        var native = values.Aggregate<uint, uint>(0, (acc, value) => acc + value);
        return sut == native;
    }

    const double Tolerance = 0.000001;

    [PropertyTest]
    public bool ShouldAvgArrayOfInt(NonEmptyArray<int> arg)
    {
        var values = arg.Item;
        var sut = MathI.Avg(values);
        var native = values.Average();
        return Math.Abs(sut - native) < Tolerance;
    }
}
