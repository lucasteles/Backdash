using BenchmarkDotNet.Running;
using nGGPO.Benchmarks;

Console.WriteLine("Start");
BenchmarkRunner.Run<GetBitStringBenchmark>();
Console.WriteLine("Finish");
