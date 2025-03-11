using BenchmarkDotNet.Running;

var switcher = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly);

#if DEBUG
switcher.Run(args, new BenchmarkDotNet.Configs.DebugInProcessConfig());
#else
switcher.Run(args);
#endif

// await new UdpClientBenchmark().Start(10, false).ConfigureAwait(false);
