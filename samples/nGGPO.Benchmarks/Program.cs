using BenchmarkDotNet.Running;
using nGGPO.Benchmarks.Cases;
using nGGPO.Network.Client;

Console.WriteLine("Start");
// BenchmarkRunner.Run<GetBitStringBenchmark>();
BenchmarkRunner.Run<UdpClientBenchmark>();
// await new UdpClientBenchmarkState(10)
//     .Start(1, UdpClientFeatureFlags.CancellableChannel)
//     .ConfigureAwait(false);