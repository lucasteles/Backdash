using Backdash.Core;
using Backdash.Data;
using Backdash.Network;
using Backdash.Options;
using Backdash.Serialization;
using Backdash.Synchronizing;
using Backdash.Synchronizing.Input.Confirmed;
using Backdash.Synchronizing.Random;
using Backdash.Synchronizing.State;

namespace Backdash.Backends;

sealed class ReplayBackend<TInput> : INetcodeSession<TInput> where TInput : unmanaged
{
    readonly Logger logger;
    readonly PlayerHandle[] fakePlayers;
    INetcodeSessionHandler callbacks;
    bool isSynchronizing = true;
    SynchronizedInput<TInput>[] syncInputBuffer = [];
    TInput[] inputBuffer = [];

    bool disposed;
    bool closed;

    readonly IReadOnlyList<ConfirmedInputs<TInput>> inputList;
    readonly SessionReplayControl controls;
    readonly IStateStore stateStore;
    readonly IChecksumProvider checksumProvider;
    readonly IDeterministicRandom<TInput> random;
    readonly Endianness endianness;

    public ReplayBackend(
        SessionReplayOptions<TInput> replayOptions,
        NetcodeOptions options,
        BackendServices<TInput> services
    )
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(replayOptions);
        ArgumentNullException.ThrowIfNull(services);

        inputList = replayOptions.InputList;
        controls = replayOptions.ReplayController ?? new();
        logger = services.Logger;
        stateStore = services.StateStore;
        checksumProvider = services.ChecksumProvider;
        random = services.DeterministicRandom;
        NumberOfPlayers = options.NumberOfPlayers;
        endianness = options.GetStateSerializationEndianness();
        callbacks = new EmptySessionHandler(logger);
        fakePlayers = Enumerable.Range(0, NumberOfPlayers)
            .Select(x => new PlayerHandle(PlayerType.Remote, x + 1, x))
            .ToArray();

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

    public SavedFrame GetCurrentSavedFrame() => stateStore.Last();

    public int NumberOfSpectators => 0;
    public int LocalPort => 0;
    public int NumberOfPlayers { get; private set; }

    public INetcodeRandom Random => random;

    public SessionMode Mode => SessionMode.Replay;
    public void DisconnectPlayer(in PlayerHandle player) { }
    public ResultCode AddLocalInput(PlayerHandle player, in TInput localInput) => ResultCode.Ok;
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

    public void SetHandler(INetcodeSessionHandler handler)
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

        ThrowIf.Assert(confirmed.Count > 0);
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

        random.UpdateSeed(CurrentFrame, inputBuffer);
        return ResultCode.Ok;
    }

    public void SaveCurrentFrame()
    {
        var currentFrame = CurrentFrame;
        ref var nextState = ref stateStore.Next();

        BinaryBufferWriter writer = new(nextState.GameState, endianness);
        callbacks.SaveState(in currentFrame, in writer);
        nextState.Frame = currentFrame;
        nextState.Checksum = checksumProvider.Compute(nextState.GameState.WrittenSpan);

        stateStore.Advance();
        logger.Write(LogLevel.Trace, $"replay: saved frame {nextState.Frame} (checksum: {nextState.Checksum:x8})");
    }

    public bool LoadFrame(in Frame frame)
    {
        if (frame.IsNull || frame == CurrentFrame)
        {
            logger.Write(LogLevel.Trace, "Skipping NOP");
            return true;
        }

        try
        {
            var savedFrame = stateStore.Load(in frame);
            logger.Write(LogLevel.Trace,
                $"Loading replay frame {savedFrame.Frame} (checksum: {savedFrame.Checksum:x8})");
            var offset = 0;
            BinaryBufferReader reader = new(savedFrame.GameState.WrittenSpan, ref offset, endianness);
            callbacks.LoadState(in frame, in reader);
            CurrentFrame = savedFrame.Frame;
            return true;
        }
        catch (NetcodeException)
        {
            controls.IsBackward = false;
            controls.Pause();
            return false;
        }
    }

    public ref readonly SynchronizedInput<TInput> GetInput(int index) =>
        ref syncInputBuffer[index];

    public ref readonly SynchronizedInput<TInput> GetInput(in PlayerHandle player) =>
        ref syncInputBuffer[player.Number - 1];
}
