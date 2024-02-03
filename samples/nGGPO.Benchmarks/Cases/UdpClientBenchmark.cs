using System.Diagnostics;
using BenchmarkDotNet.Order;
using nGGPO.Benchmarks.Network;
using nGGPO.Core;
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
    [Params(100_00, 500_00, 1_000_000)]
    public int N;

    [Benchmark]
    public async Task PinnedSendBuffer()
    {
        using UdpClientBenchmarkState data = new(pinnedSendBuffer: true);
        await data.Start(N);
    }

    [Benchmark]
    public async Task ArrayPoolBuffer()
    {
        using UdpClientBenchmarkState data = new(pinnedSendBuffer: false);
        await data.Start(N);
    }
}

sealed class UdpClientBenchmarkState : IDisposable
{
    public PingMessageHandler PongerHandler { get; }
    public PingMessageHandler PingerHandler { get; }

    public UdpClient<PingMessage> Pinger { get; }
    public UdpClient<PingMessage> Ponger { get; }

    public byte[]? PingerSendBuffer { get; private set; }
    public byte[]? PongerSendBuffer { get; private set; }

    public UdpClientBenchmarkState(bool pinnedSendBuffer)
    {
        if (pinnedSendBuffer)
        {
            PingerSendBuffer = Mem.CreatePinnedBuffer(Max.UdpPacketSize);
            PongerSendBuffer = Mem.CreatePinnedBuffer(Max.UdpPacketSize);
        }

        PingerHandler = new(nameof(Pinger), PingerSendBuffer);
        PongerHandler = new(nameof(Ponger), PongerSendBuffer);

        Pinger = Factory.CreatePingClient(PingerHandler, 9000);
        Ponger = Factory.CreatePingClient(PongerHandler, 9001);
    }

    public void Dispose()
    {
        Pinger.Dispose();
        Ponger.Dispose();
        PingerSendBuffer = null;
        PongerSendBuffer = null;
    }

    public async Task Start(
        int numberOfSpins,
        TimeSpan? timeout = null
    )
    {
        timeout ??= TimeSpan.FromSeconds(5);
        using CancellationTokenSource tokenSource = new(timeout.Value);
        var ct = tokenSource.Token;

        // ReSharper disable once AccessToDisposedClosure
        void OnProcessed(long count)
        {
            if (count >= numberOfSpins)
                tokenSource.Cancel();
        }

        PingerHandler.OnProcessed += OnProcessed;

        async Task SendMessages()
        {
            if (PongerSendBuffer is null)
                await Ponger.SendTo(Pinger.Address, PingMessage.Ping, ct);
            else
                await Ponger.SendTo(Pinger.Address, PingMessage.Ping, PongerSendBuffer, ct);
        }

        Task[] tasks =
        [
            Pinger.Start(ct),
            Ponger.Start(ct),
            SendMessages(),
        ];

        await Task.WhenAll(tasks).ConfigureAwait(false);

        PingerHandler.OnProcessed -= OnProcessed;

        Trace.Assert(PingerHandler.BadMessages is 0,
            $"** Pinger: {PingerHandler.BadMessages} bad messages");

        Trace.Assert(PingerHandler.ProcessedCount == numberOfSpins,
            $"** Pinger incomplete (Expected: {numberOfSpins}, Received: {PingerHandler.ProcessedCount})");
    }
}