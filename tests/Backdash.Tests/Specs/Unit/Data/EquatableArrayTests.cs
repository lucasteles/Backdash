using Backdash.Data;
using Backdash.Tests.TestUtils;

namespace Backdash.Tests.Specs.Unit.Data;

public class EquatableArrayTests
{
    [Fact]
    internal void ArrayInitializer()
    {
        EquatableArray<int> copy = [10, 20, 30];
        copy[0].Should().Be(10);
        copy[1].Should().Be(20);
        copy[2].Should().Be(30);
    }

    [PropertyTest]
    internal bool CloneCompareIntegers(EquatableArray<int> equatableArray)
    {
        var copy = equatableArray.Clone();
        return equatableArray == copy;
    }

    [PropertyTest]
    internal bool CloneCompareStrings(EquatableArray<string> equatableArray)
    {
        var copy = equatableArray.Clone();
        return equatableArray == copy;
    }

    [PropertyTest]
    internal bool CloneCompareFrame(EquatableArray<Frame> equatableArray)
    {
        var copy = equatableArray.Clone();
        return equatableArray == copy;
    }

    [PropertyTest]
    internal bool ComparingTwo(string[] array1, List<string> array2)
    {
        var e1 = array1.ToEquatableArray();
        var e2 = array2.ToEquatableArray();
        var cmp = e1 == e2;
        var cmpArray = array1.SequenceEqual(array2);
        return cmp == cmpArray;
    }

    [PropertyTest]
    internal bool ComparingInvariantHash(string[] array)
    {
        var hash1 = array.ToEquatableArray().GetHashCode();
        var hash2 = array.ToEquatableArray().GetHashCode();
        var hash3 = array.ToEquatableArray().GetHashCode();
        return hash1 == hash2 && hash1 == hash3 && hash2 == hash3;
    }

    [PropertyTest]
    internal bool ComparingStringHashCodes(EquatableArray<string> array1, EquatableArray<string> array2)
    {
        var cmp = array1 == array2;
        var cmpHash = array1.GetHashCode() == array2.GetHashCode();
        return cmp == cmpHash;
    }

    [PropertyTest]
    internal bool ComparingIntHashCodes(EquatableArray<int> array1, EquatableArray<int> array2)
    {
        var cmp = array1 == array2;
        var cmpHash = array1.GetHashCode() == array2.GetHashCode();
        return cmp == cmpHash;
    }

    [PropertyTest]
    internal bool ComparingComplexHashCodes(EquatableArray<Frame> array1, EquatableArray<Frame> array2)
    {
        var cmp = array1 == array2;
        var cmpHash = array1.GetHashCode() == array2.GetHashCode();
        return cmp == cmpHash;
    }

    [PropertyTest]
    internal bool Enumeration(EquatableArray<string> equatableArray)
    {
        List<string> values = [];
        foreach (var v in equatableArray)
            values.Add(v);
        return equatableArray.Length == values.Count && values.SequenceEqual(equatableArray);
    }

    [PropertyTest]
    internal bool ChangeValue(NonEmptyArray<string> values, NonEmptyString nonEmptyString)
    {
        var array = values.Item.ToEquatableArray();
        var str = nonEmptyString.Item;
        if (array[0] == str) str += "!";
        var hashBefore = array.GetHashCode();
        array[0] = str;
        var hashAfter = array.GetHashCode();
        return array[0] == str && hashBefore != hashAfter;
    }
}
