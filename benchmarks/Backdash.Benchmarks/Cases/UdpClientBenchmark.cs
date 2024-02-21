using System.Diagnostics;
using Backdash.Benchmarks.Network;
using Backdash.Core;

#pragma warning disable CS0649, AsyncFixer01, AsyncFixer02
// ReSharper disable AccessToDisposedClosure

namespace Backdash.Benchmarks.Cases;

[InProcess]
[RPlotExporter]
[MemoryDiagnoser, ExceptionDiagnoser]
[RankColumn, IterationsColumn]
public class UdpClientBenchmark
{
    [Params(1000, 50_000)]
    public int N;


    Memory<byte> pingerSendBuffer = Memory<byte>.Empty;
    Memory<byte> pongerSendBuffer = Memory<byte>.Empty;

    [GlobalSetup]
    public void Setup()
    {
        pingerSendBuffer = Mem.CreatePinnedMemory(Max.UdpPacketSize);
        pongerSendBuffer = Mem.CreatePinnedMemory(Max.UdpPacketSize);
    }

    [Benchmark]
    public async Task ArrayPoolBuffer() => await Start(N, usePinnedBuffers: false);

    [Benchmark]
    public async Task PinnedSendBuffer() => await Start(N, usePinnedBuffers: true);

    public async Task Start(
        int numberOfSpins,
        bool usePinnedBuffers,
        TimeSpan? timeout = null
    )
    {
        timeout ??= TimeSpan.FromSeconds(5);

        PingMessageHandler pingerHandler =
            new("Pinger", usePinnedBuffers ? pingerSendBuffer : null);
        PingMessageHandler pongerHandler =
            new("Ponger", usePinnedBuffers ? pongerSendBuffer : null);

        using var pinger = Factory.CreatePingClient(pingerHandler, 9000);
        using var ponger = Factory.CreatePingClient(pongerHandler, 9001);

        using CancellationTokenSource tokenSource = new(timeout.Value);
        var ct = tokenSource.Token;

        void OnProcessed(long count)
        {
            if (count >= numberOfSpins)
                tokenSource.Cancel();
        }

        pingerHandler.OnProcessed += OnProcessed;

        async Task StartSending()
        {
            if (usePinnedBuffers)
                await pinger.SendTo(ponger.Address, PingMessage.Ping, pingerSendBuffer, ct);
            else
                await pinger.SendTo(ponger.Address, PingMessage.Ping, ct);
        }

        Task[] tasks =
        [
            pinger.Start(ct),
            ponger.Start(ct),
            StartSending(),
        ];

        await Task.WhenAll(tasks).ConfigureAwait(false);

        pingerHandler.OnProcessed -= OnProcessed;

        Trace.Assert(pingerHandler.BadMessages is 0,
            $"** Pinger: {pingerHandler.BadMessages} bad messages");

        Trace.Assert(pingerHandler.ProcessedCount >= numberOfSpins,
            $"** Pinger incomplete (Expected: >= {numberOfSpins}, Received: {pingerHandler.ProcessedCount})");
    }
}