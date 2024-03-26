using Backdash.Benchmarks.Cases;
using BenchmarkDotNet.Running;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
// BenchmarkRunner.Run<GetBitStringBenchmark>();
// BenchmarkRunner.Run<UdpClientBenchmark>();
//
// SessionBenchmark bm = new();
// bm.Setup();
// try
// {
//     bm.N = 100;
//     await bm.Match2Players();
// }
// finally
// {
//     bm.CleanUp();
// }