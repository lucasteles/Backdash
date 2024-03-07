using BenchmarkDotNet.Running;
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
// BenchmarkRunner.Run<GetBitStringBenchmark>();
// BenchmarkRunner.Run<UdpClientBenchmark>();
// await new UdpClientBenchmark().Start(10, false).ConfigureAwait(false);