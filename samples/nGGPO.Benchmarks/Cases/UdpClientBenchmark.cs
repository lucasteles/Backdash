using System.Diagnostics;
using nGGPO.Benchmarks.Network;
using nGGPO.Network.Client;

// ReSharper disable once UnassignedField.Global
#pragma warning disable CS0649
#pragma warning disable AsyncFixer02

namespace nGGPO.Benchmarks.Cases;

sealed class UdpClientBenchmarkState : IDisposable
{
    public PingMessageHandler SenderHandler { get; }
    public PingMessageHandler ReceiverHandler { get; }

    public UdpClient<PingMessage> Sender { get; }
    public UdpClient<PingMessage> Receiver { get; }

    public UdpClientBenchmarkState()
    {
        SenderHandler = new(nameof(Sender));
        ReceiverHandler = new(nameof(Receiver));

        Sender = Factory.CreatePingClient(SenderHandler, 9000);
        Receiver = Factory.CreatePingClient(ReceiverHandler, 9001);
    }

    public void Dispose()
    {
        Sender.Dispose();
        Receiver.Dispose();
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

        SenderHandler.OnProcessed += OnProcessed;

        await Task.WhenAll(
            Sender.Start(flags, ct),
            Receiver.Start(flags, ct),
            Task.Run(async () =>
            {
                for (var i = 0; i < numberOfMessages; i++)
                    await Receiver.SendTo(Sender.Address, PingMessage.HandShake, ct);
            }, ct)
        );

        SenderHandler.OnProcessed -= OnProcessed;

        Trace.Assert(SenderHandler.PendingCount is 0, "Sender with pending messages");
        Trace.Assert(SenderHandler.ProcessedCount == numberOfMessages, "Sender incomplete");
        // Trace.Assert(ReceiverHandler.PendingCount is 0, "Receiver with pending messages");
        // Trace.Assert(ReceiverHandler.ProcessedCount is 0, "Receiver should be empty");
    }
}

[RPlotExporter]
[InProcess, MemoryDiagnoser]
public class UdpClientBenchmark
{
    UdpClientBenchmarkState data = default!;

    [Params(1000)]
    public int N;

    [Params(
        UdpClientFeatureFlags.CancellableChannel,
        UdpClientFeatureFlags.WaitAsync,
        UdpClientFeatureFlags.TaskYield,
        UdpClientFeatureFlags.ThreadYield
    )]
    public UdpClientFeatureFlags Feature;

    [GlobalSetup] public void Setup() => data = new();
    [GlobalCleanup] public void Cleanup() => data.Dispose();

    [Benchmark(Baseline = true)]
    public Task BaseLine() => data.Start(0, Feature);

    [Benchmark]
    public Task SingleMessage() => data.Start(N, Feature);
}