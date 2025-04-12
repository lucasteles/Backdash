using System.Diagnostics.CodeAnalysis;
using Backdash.Data;

namespace Backdash.Tests.Specs.Unit.Data;

[SuppressMessage("ReSharper", "UseObjectOrCollectionInitializer")]
public class CircularBufferTests
{
    [Fact]
    public void ShouldStartEmpty()
    {
        CircularBuffer<int> sut = new(1);
        AssertBuffer(sut, []);
        sut.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void ShouldAddOneItem()
    {
        CircularBuffer<int> sut = new(1);
        sut.Add(10);
        AssertBuffer(sut, [10]);
    }

    [Fact]
    public void ShouldAddTwoItems()
    {
        CircularBuffer<int> sut = new(3);
        sut.Add(10);
        sut.Add(20);
        AssertBuffer(sut, [10, 20]);
    }

    [Fact]
    public void ShouldAddOneMultipleItems()
    {
        CircularBuffer<int> sut = new(3);
        sut.Add(10);
        sut.Add(20);
        sut.Add(30);
        AssertBuffer(sut, [10, 20, 30]);
    }

    [Fact]
    public void ShouldPushDropFirstItemWhenFull()
    {
        CircularBuffer<int> sut = new(2);
        sut.Add(10);
        sut.Add(20);
        sut.Add(30);
        AssertBuffer(sut, [20, 30]);
        sut.IsFull.Should().BeTrue();
    }

    [Fact]
    public void ShouldPushDropFirstTwoItemsWhenFull()
    {
        CircularBuffer<int> sut = new(2);
        sut.Add(10);
        sut.Add(20);
        sut.Add(30);
        sut.Add(40);
        AssertBuffer(sut, [30, 40]);
    }

    [Fact]
    public void ShouldSoftClearBuffer()
    {
        CircularBuffer<int> sut = new(2);
        sut.Add(10);
        sut.Add(20);
        sut.Add(30);

        ref readonly var firstValue = ref sut[0];
        firstValue.Should().Be(20);

        sut.Clear();

        AssertBuffer(sut, []);
        sut.IsEmpty.Should().BeTrue();
        firstValue.Should().Be(20);
    }

    [Fact]
    public void ShouldHardClearBuffer()
    {
        CircularBuffer<int> sut = new(2);
        sut.Add(10);
        sut.Add(20);
        sut.Add(30);

        ref readonly var firstValue = ref sut[0];
        firstValue.Should().Be(20);

        sut.Clear(clearArray: true);

        AssertBuffer(sut, []);
        sut.IsEmpty.Should().BeTrue();
        firstValue.Should().Be(0);
    }

    [Fact]
    public void ShouldDropOneItem()
    {
        CircularBuffer<int> sut = new(3);
        sut.Add(10);
        sut.Add(20);
        sut.Add(30);
        sut.Drop().Should().Be(10);
        AssertBuffer(sut, [20, 30]);
    }

    [Fact]
    public void ShouldNotOverrideDrop()
    {
        CircularBuffer<int> sut = new(3);
        sut.Add(10);
        sut.Add(20);
        sut.Add(30);
        sut.Add(40);

        AssertBuffer(sut, [20, 30, 40]);
        sut.Drop().Should().Be(20);
        AssertBuffer(sut, [30, 40]);
    }

    [Fact]
    public void ShouldThrownWhenPopEmptyBuffer()
    {
        CircularBuffer<int> sut = new(3);
        var popAction = () => sut.Drop();
        popAction.Should().ThrowExactly<InvalidOperationException>("Can't pop from an empty buffer");
    }

    [Fact]
    public void ShouldTryPopEmptyBuffer()
    {
        CircularBuffer<int> sut = new(3);
        sut.TryDrop(out _).Should().BeFalse();
    }

    [Fact]
    public void ShouldTryPopBuffer()
    {
        CircularBuffer<int> sut = new(2);
        sut.Add(10);
        sut.TryDrop(out var value).Should().BeTrue();
        value.Should().Be(10);
        sut.IsEmpty.Should().BeTrue();
    }

    [Fact]
    public void ShouldHaveToString()
    {
        CircularBuffer<int> sut = new(3);
        sut.AddRange([10, 20, 30]);
        sut.ToString().Should().Be("[10, 20, 30]");
    }

    [Fact]
    public void ShouldReplaceAlmostAllValuesWhenFull()
    {
        CircularBuffer<int> sut = new(5);
        sut.AddRange([10, 20, 30, 40, 50]);
        sut.AddRange([60, 70, 80, 90]);
        AssertBuffer(sut, [50, 60, 70, 80, 90]);
    }

    [Fact]
    public void ShouldReplaceAllValuesWhenFull()
    {
        CircularBuffer<int> sut = new(5);
        sut.AddRange([10, 20, 30, 40, 50]);
        sut.AddRange([11, 22, 33, 44, 55]);
        AssertBuffer(sut, [11, 22, 33, 44, 55]);
    }

    [Fact]
    public void ShouldBeEqualByValues()
    {
        CircularBuffer<int> sutLeft = new(3);
        sutLeft.AddRange([10, 20, 30]);
        CircularBuffer<int> sutRight = new(3);
        sutRight.AddRange([10, 20, 30]);
        sutLeft.Should().Equal(sutRight);
    }

    [Fact]
    public void ShouldHaveSameHashCode()
    {
        CircularBuffer<int> sutLeft = new(3);
        sutLeft.AddRange([10, 20, 30]);
        CircularBuffer<int> sutRight = new(3);
        sutRight.AddRange([10, 20, 30]);
        sutLeft.GetHashCode().Should().Be(sutRight.GetHashCode());
    }

    [Fact]
    public void ShouldPeekLast()
    {
        CircularBuffer<int> sut = new(3);
        sut.Add(10);
        sut.Add(20);

        sut.Front().Should().Be(20);
    }

    [Fact]
    public void ShouldUnsafePeekLast()
    {
        CircularBuffer<int> sut = new(3);
        sut.Add(10);
        sut.Add(20);

        ref var last = ref sut.Front();
        last.Should().Be(20);

        last = 99;
        AssertBuffer(sut, [10, 99]);
    }

    [Fact]
    public void ShouldGetSpanLinearWithSpace()
    {
        CircularBuffer<int> sut = new(3);
        sut.Add(10);
        sut.Add(20);

        var count = sut.GetSpan(out var begin, out var end);
        int[] values = [.. begin, .. end];

        values.Should().Equal(10, 20);
        values.Length.Should().Be(count);
    }

    [Fact]
    public void ShouldGetSpanLinearFull()
    {
        CircularBuffer<int> sut = new(3);
        sut.Add(10);
        sut.Add(20);
        sut.Add(30);

        var count = sut.GetSpan(out var begin, out var end);
        int[] values = [.. begin, .. end];

        values.Should().Equal(10, 20, 30);
        values.Length.Should().Be(count);
    }

    [Fact]
    public void ShouldCopyToSpan()
    {
        CircularBuffer<int> sut = new(3);
        sut.Add(10);
        sut.Add(20);
        sut.Add(30);

        var destination = new int[3];
        sut.CopyTo(destination);
        destination.Should().Equal(10, 20, 30);
    }

    [Fact]
    public void ShouldCopyToLargeSpan()
    {
        CircularBuffer<int> sut = new(3);
        sut.Add(10);
        sut.Add(20);
        sut.Add(30);

        var destination = new int[5];
        sut.CopyTo(destination);
        destination.Should().Equal(10, 20, 30, 0, 0);
    }

    [Fact]
    public void ShouldCopyToThrowWhenShortSpan()
    {
        CircularBuffer<int> sut = new(3);
        sut.Add(10);
        sut.Add(20);
        sut.Add(30);

        var destination = new int[2];
        var action = () => sut.CopyTo(destination);
        action.Should().ThrowExactly<ArgumentException>();
    }

    [Fact]
    public void ShouldGetSpanAfterReplace()
    {
        CircularBuffer<int> sut = new(3);
        sut.Add(10);
        sut.Add(20);
        sut.Add(30);
        sut.Add(40);

        var count = sut.GetSpan(out var begin, out var end);
        int[] values = [.. begin, .. end];

        values.Should().Equal(20, 30, 40);
        values.Length.Should().Be(count);
    }

    [Fact]
    public void ShouldGetSpanAfterDrop()
    {
        CircularBuffer<int> sut = new(3);
        sut.Add(30);
        sut.Add(20);
        sut.Add(10);
        _ = sut.Drop();

        var count = sut.GetSpan(out var begin, out var end);
        int[] values = [.. begin, .. end];

        values.Should().Equal(20, 10);
        values.Length.Should().Be(count);
    }

    [Fact]
    public void ShouldGetSpanAfterFullReplace()
    {
        CircularBuffer<int> sut = new(3);
        sut.Add(10);
        sut.Add(20);
        sut.Add(30);

        sut.Add(100);
        sut.Add(200);
        sut.Add(300);

        var count = sut.GetSpan(out var begin, out var end);
        int[] values = [.. begin, .. end];

        values.Should().Equal(100, 200, 300);
        values.Length.Should().Be(count);
    }

    [Fact]
    public void ShouldGetSpanAfterFullReplacePlusOne()
    {
        CircularBuffer<int> sut = new(3);
        sut.Add(10);
        sut.Add(20);
        sut.Add(30);

        sut.Add(100);
        sut.Add(200);
        sut.Add(300);
        sut.Add(999);

        var count = sut.GetSpan(out var begin, out var end);
        int[] values = [.. begin, .. end];

        values.Should().Equal(200, 300, 999);
        values.Length.Should().Be(count);
    }

    [Fact]
    public void ShouldGetSpanAndResetZero()
    {
        CircularBuffer<int> sut = new(3);
        sut.Add(10);
        sut.Add(20);
        sut.Add(30);

        var span = sut.GetResetSpan(0);
        span.ToArray().Should().BeEmpty();
        AssertBuffer(sut, []);
    }

    [Fact]
    public void ShouldAddNewItemAfterGetResetSpan()
    {
        CircularBuffer<int> sut = new(3);
        sut.Add(10);
        sut.Add(20);
        sut.Add(30);

        _ = sut.GetResetSpan(3);
        sut.Add(40);

        AssertBuffer(sut, [20, 30, 40]);
    }

    [Fact]
    public void ShouldGetSpanAndResetValues()
    {
        CircularBuffer<int> sut = new(3);
        sut.Add(10);
        sut.Add(20);
        sut.Add(30);

        var span = sut.GetResetSpan(2);
        span.Length.Should().Be(2);

        span[0] = 100;
        span[1] = 200;

        AssertBuffer(sut, [100, 200]);
    }

    [Fact]
    public void ShouldDiscardLastItems()
    {
        CircularBuffer<int> sut = new(5);
        sut.Add(10);
        sut.Add(20);
        sut.Add(30);
        sut.Add(50);
        sut.Add(60);

        sut.Discard(3);
        AssertBuffer(sut, [50, 60]);

        sut.Add(70);
        sut.Add(80);
        AssertBuffer(sut, [50, 60, 70, 80]);
    }

    [Fact]
    public void ShouldDiscardAll()
    {
        CircularBuffer<int> sut = new(5);
        sut.Add(10);
        sut.Add(20);
        sut.Add(30);
        sut.Discard(10);
        AssertBuffer(sut, []);

        sut.Add(100);
        sut.Add(200);
        AssertBuffer(sut, [100, 200]);
    }

    [Fact]
    public void ShouldAdvance()
    {
        CircularBuffer<int> sut = new(3);
        sut.Add(10);
        sut.Add(20);
        sut.Next() = 30;
        sut.Advance();
        AssertBuffer(sut, [10, 20, 30]);
    }

    [Fact]
    public void ShouldAdvanceLast()
    {
        CircularBuffer<int> sut = new(3);
        sut.Add(10);
        sut.Add(20);
        sut.Add(30);
        sut.Next() = 40;
        sut.Advance();
        AssertBuffer(sut, [20, 30, 40]);
    }

    [Fact]
    public void ShouldAdvanceMany()
    {
        CircularBuffer<int> sut = new(3);
        sut.Add(10);
        sut.Advance(2);
        sut.Front() = 20;
        AssertBuffer(sut, [10, 0, 20]);
    }

    [Fact]
    public void ShouldAdvanceManyLast()
    {
        CircularBuffer<int> sut = new(3);
        sut.Add(10);
        sut.Next() = 20;
        sut.Advance(3);
        sut.Front() = 30;
        AssertBuffer(sut, [20, 0, 30]);
    }

    [Fact]
    public void ShouldAdvanceNegative()
    {
        CircularBuffer<int> sut = new(3);
        sut.Add(10);
        sut.Add(20);
        sut.Advance(-1);
        AssertBuffer(sut, [10]);
    }

    [Fact]
    public void ShouldAdvanceNegativeMany()
    {
        CircularBuffer<int> sut = new(5);
        sut.Add(10);
        sut.Add(20);
        sut.Add(30);
        sut.Drop();
        sut.Drop();
        sut.Add(40);
        AssertBuffer(sut, [30, 40]);
        sut.Add(50);
        sut.Advance(-2);
        sut.Add(60);
        AssertBuffer(sut, [30, 60]);
    }

    [Fact]
    public void ShouldDiscardMany()
    {
        CircularBuffer<int> sut = new(5);
        sut.Add(10);
        sut.Add(20);
        sut.Add(30);
        sut.Drop();
        sut.Drop();
        sut.Add(40);
        AssertBuffer(sut, [30, 40]);
        sut.Add(50);
        sut.Discard(2);
        sut.Add(60);
        AssertBuffer(sut, [50, 60]);
    }

    static void AssertBuffer<T>(CircularBuffer<T> buffer, T[] expected)
    {
        buffer.Size.Should().Be(expected.Length);
        buffer.ToArray().Should().BeEquivalentTo(expected, "to array");

        List<T> list = new(buffer.Size);
        foreach (var item in buffer)
            list.Add(item);
        list.Should().BeEquivalentTo(expected, "foreach");

        for (var i = 0; i < buffer.Size; i++)
            buffer[i].Should().Be(expected[i], $"by index {i}");
    }
}

#pragma warning restore IDE0028
