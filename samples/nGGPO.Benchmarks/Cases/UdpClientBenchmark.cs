using System.Diagnostics;
using BenchmarkDotNet.Order;
using nGGPO.Benchmarks.Network;
using nGGPO.Network.Client;

#pragma warning disable CS0649
#pragma warning disable AsyncFixer02

namespace nGGPO.Benchmarks.Cases;

[InProcess]
[RPlotExporter]
[MemoryDiagnoser, ThreadingDiagnoser, ExceptionDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn, IterationsColumn]
public class UdpClientBenchmark
{
    [Params(5000)]
    public int N;

    [Benchmark(Baseline = true)]
    public async Task BaseLine()
    {
        using UdpClientBenchmarkState data = new();
        await data.Start(0);
    }

    [Benchmark(Baseline = true)]
    public async Task PingLoop()
    {
        using UdpClientBenchmarkState data = new();
        await data.Start(N);
    }
}

sealed class UdpClientBenchmarkState : IDisposable
{
    public PingMessageHandler PongerHandler { get; }
    public PingMessageHandler PingerHandler { get; }

    public UdpClient<PingMessage> Pinger { get; }
    public UdpClient<PingMessage> Ponger { get; }

    public UdpClientBenchmarkState(long spinCount = 10)
    {
        PongerHandler = new(nameof(Ponger));
        PingerHandler = new(nameof(Pinger), spinCount);

        Pinger = Factory.CreatePingClient(PongerHandler, 9000);
        Ponger = Factory.CreatePingClient(PingerHandler, 9001);
    }

    public void Dispose()
    {
        Pinger.Dispose();
        Ponger.Dispose();
    }

    public async Task Start(
        int numberOfMessages,
        TimeSpan? timeout = null
    )
    {
        timeout ??= TimeSpan.FromSeconds(10);
        using CancellationTokenSource tokenSource = new(timeout.Value);
        var ct = tokenSource.Token;

        // ReSharper disable once AccessToDisposedClosure
        void OnProcessed(long count)
        {
            if (count >= numberOfMessages)
                tokenSource.Cancel();
        }

        PingerHandler.OnProcessed += OnProcessed;

        Task[] tasks =
        [
            Pinger.Start(ct),
            Ponger.Start(ct),
            ..Enumerable.Range(0, numberOfMessages).Select(_ =>
                Ponger.SendTo(Pinger.Address, PingMessage.Ping, ct).AsTask()),
        ];

        await Task.WhenAll(tasks).ConfigureAwait(false);

        PingerHandler.OnProcessed -= OnProcessed;

        if (PingerHandler.ProcessedCount != numberOfMessages)
            Console.WriteLine(
                $"** Pinger incomplete (Expected: {numberOfMessages}, Received: {PingerHandler.ProcessedCount})");
    }
}