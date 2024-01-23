using System.Drawing;
using nGGPO.Tests.Serialization;

namespace nGGPO.Tests.Utils;

[Serializable, AttributeUsage(AttributeTargets.Method)]
public sealed class PropertyTestAttribute : FsCheck.Xunit.PropertyAttribute
{
    public PropertyTestAttribute()
    {
        QuietOnSuccess = true;
        Arbitrary = [typeof(MyGenerators)];
    }
}

[Serializable]
public class MyGenerators
{
    public static Arbitrary<Point> PointGenerator() => Arb.From(
        from x in Arb.From<int>().Generator
        from y in Arb.From<int>().Generator
        select new Point(x, y)
    );

    public static Arbitrary<SimpleStructData> SimpleStructDataGenerator()
    {
        var generator =
            from f1 in Arb.From<int>().Generator
            from f2 in Arb.From<uint>().Generator
            from f3 in Arb.From<ulong>().Generator
            from f4 in Arb.From<long>().Generator
            from f5 in Arb.From<short>().Generator
            from f6 in Arb.From<ushort>().Generator
            from f7 in Arb.From<byte>().Generator
            from f8 in Arb.From<sbyte>().Generator
            from f9 in Arb.From<Point>().Generator
            select new SimpleStructData
            {
                Field1 = f1,
                Field2 = f2,
                Field3 = f3,
                Field4 = f4,
                Field5 = f5,
                Field6 = f6,
                Field7 = f7,
                Field8 = f8,
                Field9 = f9,
            };

        return Arb.From(generator);
    }

    public static Arbitrary<MarshalStructData> MarshalStructDataGenerator()
    {
        var generator =
            from f1 in Arb.From<int>().Generator
            from f2 in Arb.From<long>().Generator
            from f3 in Arb.From<byte>().Generator
            from fArray in Arb.From<byte[]>().Generator.Where(a => a.Length is 10)
            select new MarshalStructData
            {
                Field1 = f1,
                Field2 = f2,
                Field3 = f3,
                FieldArray = fArray,
            };

        return Arb.From(generator);
    }
}
