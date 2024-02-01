using BenchmarkDotNet.Running;
using nGGPO.Benchmarks.Cases;

Console.WriteLine("Start");
// BenchmarkRunner.Run<GetBitStringBenchmark>();
BenchmarkRunner.Run<UdpClientBenchmark>();
// await new UdpClientBenchmarkState().Start(1, TimeSpan.FromSeconds(10)).ConfigureAwait(false);