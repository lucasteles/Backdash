using System.Diagnostics;
using Backdash.Core;
using Backdash.Data;
using Backdash.Network;
using Backdash.Synchronizing;
using Backdash.Synchronizing.Input.Confirmed;
using Backdash.Synchronizing.Random;
using Backdash.Synchronizing.State;

namespace Backdash.Backends;

sealed class ReplayBackend<TInput, TGameState> : IRollbackSession<TInput, TGameState>
    where TInput : unmanaged
    where TGameState : notnull, new()
{
    readonly Logger logger;
    readonly PlayerHandle[] fakePlayers;
    IRollbackHandler<TGameState> callbacks;
    readonly IDeterministicRandom deterministicRandom;
    bool isSynchronizing = true;
    SynchronizedInput<TInput>[] syncInputBuffer = [];
    TInput[] inputBuffer = [];

    bool disposed;
    bool closed;

    readonly IReadOnlyList<ConfirmedInputs<TInput>> inputList;
    readonly SessionReplayControl controls;
    readonly bool useInputSeedForRandom;
    readonly IStateStore<TGameState> stateStore;
    readonly IChecksumProvider<TGameState> checksumProvider;

    public ReplayBackend(int numberOfPlayers,
        bool useInputSeedForRandom,
        IReadOnlyList<ConfirmedInputs<TInput>> inputList,
        SessionReplayControl controls,
        BackendServices<TInput, TGameState> services)
    {
        ArgumentNullException.ThrowIfNull(services);

        this.inputList = inputList;
        this.controls = controls;
        this.useInputSeedForRandom = useInputSeedForRandom;
        logger = services.Logger;
        stateStore = services.StateStore;
        checksumProvider = services.ChecksumProvider;
        deterministicRandom = services.DeterministicRandom;
        NumberOfPlayers = numberOfPlayers;
        fakePlayers = Enumerable.Range(0, numberOfPlayers)
            .Select(x => new PlayerHandle(PlayerType.Remote, x + 1, x)).ToArray();

        callbacks = new EmptySessionHandler<TGameState>(logger);

        stateStore.Initialize(controls.MaxBackwardFrames);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Close();
        logger.Dispose();
    }

    public void Close()
    {
        if (closed) return;
        closed = true;
        logger.Write(LogLevel.Information, "Shutting down connections");
        callbacks.OnSessionClose();
    }

    public Frame CurrentFrame { get; private set; } = Frame.Zero;
    public FrameSpan RollbackFrames => FrameSpan.Zero;
    public FrameSpan FramesBehind => FrameSpan.Zero;
    public int NumberOfPlayers { get; private set; }
    public int NumberOfSpectators => 0;
    public IDeterministicRandom Random => deterministicRandom;
    public SessionMode Mode => SessionMode.Replaying;
    public void DisconnectPlayer(in PlayerHandle player) { }
    public ResultCode AddLocalInput(PlayerHandle player, TInput localInput) => ResultCode.Ok;
    public IReadOnlyCollection<PlayerHandle> GetPlayers() => fakePlayers;
    public IReadOnlyCollection<PlayerHandle> GetSpectators() => [];

    public void BeginFrame() { }

    public void AdvanceFrame()
    {
        if (controls.IsPaused)
            return;

        if (controls.IsBackward)
        {
            LoadFrame(CurrentFrame.Previous());
        }
        else
        {
            CurrentFrame++;
            SaveCurrentFrame();
        }

        CurrentFrame = Frame.Clamp(CurrentFrame, 0, inputList.Count);
        logger.Write(LogLevel.Debug, $"[End Frame {CurrentFrame}]");
    }

    public PlayerConnectionStatus GetPlayerStatus(in PlayerHandle player) => PlayerConnectionStatus.Connected;
    public ResultCode AddPlayer(Player player) => ResultCode.NotSupported;

    public IReadOnlyList<ResultCode> AddPlayers(IReadOnlyList<Player> players) =>
        Enumerable.Repeat(ResultCode.NotSupported, players.Count).ToArray();

    public bool GetNetworkStatus(in PlayerHandle player, ref PeerNetworkStats info) => true;

    public void SetFrameDelay(PlayerHandle player, int delayInFrames) { }

    public void Start(CancellationToken stoppingToken = default)
    {
        callbacks.OnSessionStart();
        isSynchronizing = false;
    }

    public Task WaitToStop(CancellationToken stoppingToken = default) => Task.CompletedTask;

    public void SetHandler(IRollbackHandler<TGameState> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        callbacks = handler;
    }

    public ResultCode SynchronizeInputs()
    {
        if (isSynchronizing || controls.IsPaused)
            return ResultCode.NotSynchronized;

        if (CurrentFrame.Number >= inputList.Count)
            return ResultCode.NotSynchronized;

        var confirmed = inputList[CurrentFrame.Number];

        if (confirmed.Count is 0 && CurrentFrame == Frame.Zero)
            return ResultCode.NotSynchronized;

        Trace.Assert(confirmed.Count > 0);
        NumberOfPlayers = confirmed.Count;

        if (syncInputBuffer.Length != NumberOfPlayers)
        {
            Array.Resize(ref syncInputBuffer, NumberOfPlayers);
            Array.Resize(ref inputBuffer, syncInputBuffer.Length);
        }

        for (var i = 0; i < NumberOfPlayers; i++)
        {
            syncInputBuffer[i] = new(confirmed.Inputs[i], false);
            inputBuffer[i] = confirmed.Inputs[i];
        }

        var inputPopCount = useInputSeedForRandom ? Mem.PopCount<TInput>(inputBuffer.AsSpan()) : 0;
        deterministicRandom.UpdateSeed(CurrentFrame.Number, inputPopCount);

        return ResultCode.Ok;
    }

    public void SaveCurrentFrame()
    {
        var currentFrame = CurrentFrame;
        ref var nextState = ref stateStore.GetCurrent();
        callbacks.ClearState(ref nextState);
        callbacks.SaveState(in currentFrame, ref nextState);
        var checksum = checksumProvider.Compute(in nextState);
        ref readonly var next = ref stateStore.SaveCurrent(in currentFrame, in checksum);
        logger.Write(LogLevel.Trace, $"replay: saved frame {next.Frame} (checksum: {next.Checksum}).");
    }

    public void LoadFrame(in Frame frame)
    {
        if (frame.IsNull || frame == CurrentFrame)
        {
            logger.Write(LogLevel.Trace, "Skipping NOP.");
            return;
        }

        try
        {
            ref readonly var savedFrame = ref stateStore.Load(frame);
            logger.Write(LogLevel.Trace,
                $"Loading replay frame {savedFrame.Frame} (checksum: {savedFrame.Checksum})");
            callbacks.LoadState(in frame, in savedFrame.GameState);
            CurrentFrame = savedFrame.Frame;
        }
        catch (NetcodeException)
        {
            controls.IsBackward = false;
            controls.Pause();
        }
    }

    public ref readonly SynchronizedInput<TInput> GetInput(int index) =>
        ref syncInputBuffer[index];

    public ref readonly SynchronizedInput<TInput> GetInput(in PlayerHandle player) =>
        ref syncInputBuffer[player.Number - 1];
}
