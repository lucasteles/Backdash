using BenchmarkDotNet.Running;
using Backdash.Benchmarks.Cases;

Console.WriteLine("Start");
// BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

// BenchmarkRunner.Run<GetBitStringBenchmark>();
BenchmarkRunner.Run<UdpClientBenchmark>();
// await new UdpClientBenchmark().Start(10, false).ConfigureAwait(false);