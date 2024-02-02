using System.Diagnostics;
using nGGPO.Benchmarks.Network;
using nGGPO.Network.Client;

#pragma warning disable CS0649
#pragma warning disable AsyncFixer02

namespace nGGPO.Benchmarks.Cases;

[InProcess]
[RPlotExporter]
[MemoryDiagnoser, ThreadingDiagnoser]
public class UdpClientBenchmark
{
    [Params(10_000)]
    public int N;

    [Params(
        UdpClientFeatureFlags.CancellableChannel,
        UdpClientFeatureFlags.WaitAsync,
        UdpClientFeatureFlags.TaskYield,
        UdpClientFeatureFlags.ThreadYield
    )]
    public UdpClientFeatureFlags Feature;

    [Benchmark]
    public async Task PingLoop()
    {
        using UdpClientBenchmarkState data = new();
        await data.Start(N, Feature);
    }
}

sealed class UdpClientBenchmarkState : IDisposable
{
    public PingMessageHandler PongerHandler { get; }
    public PingMessageHandler PingerHandler { get; }

    public UdpClient<PingMessage> Pinger { get; }
    public UdpClient<PingMessage> Ponger { get; }

    public UdpClientBenchmarkState(long spinCount = 10_000)
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
        UdpClientFeatureFlags flags
    )
    {
        var timeout = TimeSpan.FromSeconds(5);
        using CancellationTokenSource tokenSource = new(timeout);
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
            Pinger.Start(flags, ct),
            Ponger.Start(flags, ct),
            ..Enumerable.Range(0, numberOfMessages).Select(_ =>
                Ponger.SendTo(Pinger.Address, PingMessage.Ping, ct).AsTask()),
        ];

        await Task.WhenAll(tasks).ConfigureAwait(false);

        PingerHandler.OnProcessed -= OnProcessed;

        Trace.Assert(PingerHandler.ProcessedCount == numberOfMessages, "Sender incomplete");
    }
}