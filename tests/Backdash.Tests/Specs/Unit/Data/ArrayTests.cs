using Backdash.Data;

namespace Backdash.Tests.Specs.Unit.Data;

public class ArrayTests
{
    [PropertyTest]
    internal bool CloneCompareIntegers(Array<int> array)
    {
        var copy = array.Clone();
        return array == copy;
    }

    [PropertyTest]
    internal bool CloneCompareStrings(Array<string> array)
    {
        var copy = array.Clone();
        return array == copy;
    }

    [PropertyTest]
    internal bool CloneCompareFrame(Array<Frame> array)
    {
        var copy = array.Clone();
        return array == copy;
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
    internal bool ComparingStringHashCodes(Array<string> array1, Array<string> array2)
    {
        var cmp = array1 == array2;
        var cmpHash = array1.GetHashCode() == array2.GetHashCode();
        return cmp == cmpHash;
    }

    [PropertyTest]
    internal bool ComparingIntHashCodes(Array<int> array1, Array<int> array2)
    {
        var cmp = array1 == array2;
        var cmpHash = array1.GetHashCode() == array2.GetHashCode();
        return cmp == cmpHash;
    }

    [PropertyTest]
    internal bool ComparingComplexHashCodes(Array<Frame> array1, Array<Frame> array2)
    {
        var cmp = array1 == array2;
        var cmpHash = array1.GetHashCode() == array2.GetHashCode();
        return cmp == cmpHash;
    }

    [PropertyTest]
    internal bool Enumeration(Array<string> array)
    {
        List<string> values = [];

        foreach (var v in array)
            values.Add(v);

        return array.Length == values.Count && values.SequenceEqual(array);
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
