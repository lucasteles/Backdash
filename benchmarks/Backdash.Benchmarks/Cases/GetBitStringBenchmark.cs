// ReSharper disable UnassignedField.Global
using Backdash.Core;
namespace Backdash.Benchmarks.Cases;
[RPlotExporter]
[InProcess, MemoryDiagnoser]
public class GetBitStringBenchmark
{
    byte[] data = [];
    [Params(10, 100, 1000, 10_000)]
    public int N;
    [GlobalSetup]
    public void Setup()
    {
        Random random = new(42);
        data = new byte[N];
        random.NextBytes(data);
    }
    static string ConvertToBase2(byte[] bytes) =>
        string.Join('-', bytes.Select(n => Convert.ToString(n, 2).PadLeft(8, '0')));
    [Benchmark]
    public string GetBitString() => Mem.GetBitString(data);
    [Benchmark]
    public string ConvertAndLinq() => ConvertToBase2(data);
}