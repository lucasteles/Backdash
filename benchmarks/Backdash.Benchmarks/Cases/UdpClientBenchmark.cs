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


    Memory<byte> pingerPinnedBuffer = Memory<byte>.Empty;
    Memory<byte> pongerPinnedBuffer = Memory<byte>.Empty;

    [GlobalSetup]
    public void Setup()
    {
        pingerPinnedBuffer = Mem.CreatePinnedMemory(Max.UdpPacketSize);
        pongerPinnedBuffer = Mem.CreatePinnedMemory(Max.UdpPacketSize);
    }

    [Benchmark]
    public async Task ArrayPoolBuffer() => await Start(N, null, null);

    [Benchmark]
    public async Task PinnedSendBuffer() => await Start(N, pingerPinnedBuffer, pongerPinnedBuffer);

    public async Task Start(
        int numberOfSpins,
        Memory<byte> pingerSendBuffer,
        Memory<byte> pongerSendBuffer,
        TimeSpan? timeout = null
    )
    {
        timeout ??= TimeSpan.FromSeconds(5);

        using var pinger = Factory.CreateUdpClient(9000, out var pingerObservers);
        using var ponger = Factory.CreateUdpClient(9001, out var pongerObservers);

        PingMessageHandler pingerHandler = new("Pinger", pinger, pingerSendBuffer);
        PingMessageHandler pongerHandler = new("Ponger", ponger, pongerSendBuffer);

        pingerObservers.Add(pingerHandler);
        pongerObservers.Add(pongerHandler);

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
            if (pingerSendBuffer.IsEmpty)
                await pinger.SendTo(ponger.Address, PingMessage.Ping, ct);
            else
                await pinger.SendTo(ponger.Address, PingMessage.Ping, pingerSendBuffer, ct);
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