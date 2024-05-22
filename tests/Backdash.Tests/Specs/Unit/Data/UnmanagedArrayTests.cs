using Backdash.Data;
using Backdash.Tests.TestUtils;

namespace Backdash.Tests.Specs.Unit.Data;

public class UnmanagedArrayTests
{
    [Fact]
    internal void ArrayInitializer()
    {
        using UnmanagedArray<int> arr = new(3);
        arr[0] = 10;
        arr[1] = 20;
        arr[2] = 30;

        arr.ToArray().Should().BeEquivalentTo([10, 20, 30]);
    }

    [Fact]
    internal void ArrayValueInitializer()
    {
        using UnmanagedArray<int> arr = [10, 20, 30];
        arr[0].Should().Be(10);
        arr[1].Should().Be(20);
        arr[2].Should().Be(30);
    }

    [PropertyTest]
    public bool CloneCompareIntegers(int[] nums)
    {
        using UnmanagedArray<int> equatableArray = new(nums);
        using var copy = equatableArray.Clone();
        return equatableArray == copy;
    }

    [PropertyTest]
    internal bool CloneCompareFrame(Frame[] frames)
    {
        using UnmanagedArray<Frame> equatableArray = new(frames);
        using var copy = equatableArray.Clone();
        return equatableArray == copy;
    }

    [PropertyTest]
    internal bool ComparingIntHashCodes(int[] int1, int[] int2)
    {
        using UnmanagedArray<int> array1 = new(int1);
        using UnmanagedArray<int> array2 = new(int2);
        var cmp = array1 == array2;
        var cmpHash = array1.GetHashCode() == array2.GetHashCode();

        return cmp == cmpHash;
    }

    [PropertyTest]
    internal bool ComparingComplexHashCodes(Frame[] frames1, Frame[] frames2)
    {
        using UnmanagedArray<Frame> array1 = new(frames1);
        using UnmanagedArray<Frame> array2 = new(frames2);
        var cmp = array1 == array2;
        var cmpHash = array1.GetHashCode() == array2.GetHashCode();
        return cmp == cmpHash;
    }

    [PropertyTest]
    internal bool ChangeValue(NonEmptyArray<int> values, PositiveInt positiveInt)
    {
        using UnmanagedArray<int> array = new(values.Item);
        var value = positiveInt.Item;
        if (array[0] == value) value += 1;
        var hashBefore = array.GetHashCode();
        array[0] = value;
        var hashAfter = array.GetHashCode();
        return array[0] == value && hashBefore != hashAfter;
    }
}
