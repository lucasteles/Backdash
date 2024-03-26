using System.Net;
using Backdash.Core;
using Backdash.Data;

#pragma warning disable CS0649, AsyncFixer01, AsyncFixer02
// ReSharper disable AccessToDisposedClosure
namespace Backdash.Benchmarks.Cases;

[Flags]
public enum GameInput
{
    None = 0,
    Up = 1 << 0,
    Down = 1 << 1,
    Left = 1 << 2,
    Right = 1 << 3,
}

public record struct GameState;

[InProcess]
[RPlotExporter]
[MemoryDiagnoser, ExceptionDiagnoser, ThreadingDiagnoser]
[RankColumn, IterationsColumn]
public class SessionBenchmark
{
    [Params(10_000)]
    public int N;

    IRollbackSession<GameInput, GameState> peer1 = null!;
    IRollbackSession<GameInput, GameState> peer2 = null!;
    CancellationTokenSource cts = null!;

    [GlobalSetup]
    public void Setup()
    {
        cts = new();

        peer1 = RollbackNetcode.CreateSession<GameInput, GameState>(9000,
            new() {Log = new(LogLevel.None)}, new()
            {
            });

        peer2 = RollbackNetcode.CreateSession<GameInput, GameState>(9001,
            new() {Log = new(LogLevel.None)}, new()
            {
            });

        peer1.AddPlayer(new LocalPlayer(1));
        peer1.AddPlayer(new RemotePlayer(2, IPAddress.Loopback, 9001));

        peer2.AddPlayer(new RemotePlayer(1, IPAddress.Loopback, 9000));
        peer2.AddPlayer(new LocalPlayer(2));

        peer1.SetHandler(new Handler(peer1));
        peer2.SetHandler(new Handler(peer2));

        peer1.Start(cts.Token);
        peer2.Start(cts.Token);
    }

    [GlobalCleanup]
    public void CleanUp()
    {
        cts.Cancel();
        cts.Dispose();
    }

    [Benchmark]
    public async Task Match2Players()
    {
        var p1 = peer1.GetPlayers().Single(x => x.IsLocal());
        var p2 = peer2.GetPlayers().Single(x => x.IsLocal());

        var input = GameInput.Up | GameInput.Right;

        await Task.WhenAll(
            Task.Run(() =>
            {
                while (peer1.CurrentFrame.Number < N)
                {
                    peer1.BeginFrame();
                    if (peer1.AddLocalInput(p1, input) is ResultCode.Ok &&
                        peer1.SynchronizeInputs() is ResultCode.Ok)
                        peer1.AdvanceFrame();
                }
            }),
            Task.Run(() =>
            {
                while (peer2.CurrentFrame.Number < N)
                {
                    peer2.BeginFrame();
                    if (peer2.AddLocalInput(p2, input) is ResultCode.Ok &&
                        peer2.SynchronizeInputs() is ResultCode.Ok)
                        peer2.AdvanceFrame();
                }
            })
        );

        await cts.CancelAsync();
        await Task.WhenAll(peer1.WaitToStop(), peer2.WaitToStop());
    }
}

sealed class Handler(IRollbackSession<GameInput> session) : IRollbackHandler<GameState>
{
    public void OnSessionStart()
    {
    }

    public void OnSessionClose()
    {
    }

    public void SaveState(in Frame frame, ref GameState state)
    {
    }

    public void LoadState(in Frame frame, in GameState gameState)
    {
    }

    public void AdvanceFrame()
    {
        session.AdvanceFrame();
    }

    public void TimeSync(FrameSpan framesAhead)
    {
    }

    public void OnPeerEvent(PlayerHandle player, PeerEventInfo evt)
    {
    }
}