using System.Diagnostics;
using System.Net;
using Backdash.Benchmarks.Network;
using Backdash.Core;
using Backdash.Data;
using Backdash.Sync.Input;
using Backdash.Sync.State;

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
[MemoryDiagnoser, ExceptionDiagnoser]
[RankColumn, IterationsColumn]
public class SessionBenchmark
{
    [Params(1000)]
    public int N;

    // readonly RandomInputGenerator<GameInput> inputGenerator = new(new(42));

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

        var input = GameInput.None;
        while (true)
        {
            peer1.BeginFrame();
            peer2.BeginFrame();

            if (peer1.AddLocalInput(p1, input) is ResultCode.NotSynchronized)
                await Task.Delay(100);
            else
            {
                peer1.AdvanceFrame();
                break;
            }

            if (peer2.AddLocalInput(p2, input) is ResultCode.NotSynchronized)
                await Task.Delay(100);
            else
            {
                peer2.AdvanceFrame();
                break;
            }
        }

        for (var i = 0; i < N; i++)
        {
            peer1.BeginFrame();
            if (peer1.AddLocalInput(p1, input) is ResultCode.Ok &&
                peer1.SynchronizeInputs() is ResultCode.Ok)
                peer1.AdvanceFrame();

            peer2.BeginFrame();
            if (peer2.AddLocalInput(p2, input) is ResultCode.Ok &&
                peer2.SynchronizeInputs() is ResultCode.Ok)
                peer2.AdvanceFrame();
        }

        await cts.CancelAsync();
        await Task.WhenAll(peer1.WaitToStop(), peer2.WaitToStop());
    }
}

sealed class FakeStateStore : IStateStore<GameState>
{
    public void Dispose()
    {
    }

    public void Initialize(int size)
    {
    }

    SavedFrame<GameState> savedFrame = new(Frame.Zero, new(), 0);

    public ref readonly SavedFrame<GameState> Load(Frame frame)
    {
        savedFrame.Frame = frame;
        return ref savedFrame;
    }

    public ref readonly SavedFrame<GameState> Last()
    {
        return ref savedFrame;
    }

    public ref GameState GetCurrent()
    {
        return ref savedFrame.GameState;
    }

    public ref readonly SavedFrame<GameState> SaveCurrent(in Frame frame, in int checksum)
    {
        savedFrame.Frame = frame;
        return ref savedFrame;
    }
}