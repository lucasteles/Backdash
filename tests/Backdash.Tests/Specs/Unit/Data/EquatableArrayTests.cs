using Backdash.Data;

namespace Backdash.Tests.Specs.Unit.Data;

public class EquatableArrayTests
{
    [PropertyTest]
    internal bool CloneCompareIntegers(EquatableArray<int> array)
    {
        var copy = array.Clone();
        return array == copy;
    }

    [PropertyTest]
    internal bool CloneCompareStrings(EquatableArray<string> array)
    {
        var copy = array.Clone();
        return array == copy;
    }

    [PropertyTest]
    internal bool CloneCompareFrame(EquatableArray<Frame> array)
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
    internal bool Enumeration(EquatableArray<string> array)
    {
        List<string> values = [];

        foreach (var v in array)
            values.Add(v);

        return array.Length == values.Count && values.SequenceEqual(array);
    }
}
