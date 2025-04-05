using Backdash.Synchronizing.Random;

namespace Backdash.Tests.Specs.Unit.Sync.Input;

public class DeterministicRandomTests
{
    [Fact]
    public void ShouldBeReturnEqualValuesInOrder()
    {
        XorShiftRandom<int> random1 = new();
        XorShiftRandom<int> random2 = new();

        random1.UpdateSeed((Frame)1, []);
        random2.UpdateSeed((Frame)1, []);

        Assert.Equal(random1.Next(), random2.Next());
        Assert.Equal(random1.Next(), random2.Next());
        Assert.Equal(random1.Next(), random2.Next());

        random1.UpdateSeed((Frame)2, [1]);
        random2.UpdateSeed((Frame)2, [1]);

        Assert.Equal(random1.Next(), random2.Next());
        Assert.Equal(random1.Next(), random2.Next());
        Assert.Equal(random1.Next(), random2.Next());
    }

    [Fact]
    public void ShouldBeDeterministic()
    {
        XorShiftRandom<int> random = new();
        random.UpdateSeed((Frame)1, []);

        Span<uint> values1 =
        [
            random.Next(),
            random.Next(),
            random.Next(),
        ];

        random.UpdateSeed((Frame)1, []);
        Span<uint> values2 =
        [
            random.Next(),
            random.Next(),
            random.Next(),
        ];

        Assert.True(values1.SequenceEqual(values2));
    }

    [Fact]
    public void ShouldExtraStateSeedChangeTheResult()
    {
        XorShiftRandom<int> random1 = new();
        XorShiftRandom<int> random2 = new();
        XorShiftRandom<int> random3 = new();
        XorShiftRandom<int> random4 = new();

        random1.UpdateSeed((Frame)1, [], 1);
        random2.UpdateSeed((Frame)1, [], 2);
        random3.UpdateSeed((Frame)1, [255], 1);
        random4.UpdateSeed((Frame)1, [255], 2);

        uint[] values =
        [
            random1.Next(),
            random2.Next(),
            random3.Next(),
            random4.Next(),
        ];

        Assert.Distinct(values);
    }
}
