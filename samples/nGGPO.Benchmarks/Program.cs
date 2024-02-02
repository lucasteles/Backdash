using BenchmarkDotNet.Running;
using nGGPO.Benchmarks.Cases;
using nGGPO.Network.Client;

Console.WriteLine("Start");
// BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

// BenchmarkRunner.Run<GetBitStringBenchmark>();
BenchmarkRunner.Run<UdpClientBenchmark>();
// await new UdpClientBenchmarkState(10)
//     .Start(1, UdpClientFeatureFlag.CancellableChannel)
//     .ConfigureAwait(false);