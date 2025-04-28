using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Backdash.Core;
using Backdash.Network;
using Backdash.Options;
using Backdash.Serialization;
using Backdash.Synchronizing;
using Backdash.Synchronizing.Input.Confirmed;
using Backdash.Synchronizing.Random;
using Backdash.Synchronizing.State;

namespace Backdash.Backends;

sealed class ReplaySession<TInput> : INetcodeSession<TInput> where TInput : unmanaged
{
    readonly Logger logger;
    readonly FrozenSet<NetcodePlayer> fakePlayers;
    readonly FrozenDictionary<Guid, NetcodePlayer> playerMap;
    INetcodeSessionHandler callbacks;
    bool isSynchronizing = true;
    SynchronizedInput<TInput>[] syncInputBuffer = [];
    TInput[] inputBuffer = [];

    bool disposed;
    bool closed;

    readonly IReadOnlyList<ConfirmedInputs<TInput>> inputList;
    readonly IStateStore stateStore;
    readonly IChecksumProvider checksumProvider;
    readonly IDeterministicRandom<TInput> random;
    readonly Endianness endianness;
    public int FixedFrameRate { get; }
    public SessionReplayControl ReplayController { get; }

    public ReplaySession(
        SessionReplayOptions<TInput> replayOptions,
        NetcodeOptions options,
        SessionServices<TInput> services
    )
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(replayOptions);
        ArgumentNullException.ThrowIfNull(services);

        inputList = replayOptions.InputList;
        ReplayController = replayOptions.ReplayController ?? new();
        logger = services.Logger;
        stateStore = services.StateStore;
        checksumProvider = services.ChecksumProvider;
        random = services.DeterministicRandom;
        NumberOfPlayers = options.NumberOfPlayers;
        FixedFrameRate = options.FrameRate;
        endianness = options.GetStateSerializationEndianness();
        callbacks = services.SessionHandler;

        fakePlayers = Enumerable.Range(0, NumberOfPlayers)
            .Select(x => new NetcodePlayer((sbyte)x, PlayerType.Remote))
            .ToFrozenSet();
        playerMap = fakePlayers.ToFrozenDictionary(x => x.Id, x => x);
        stateStore.Initialize(ReplayController.MaxBackwardFrames);
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

    public bool IsInRollback => false;
    public SavedFrame GetCurrentSavedFrame() => stateStore.Last();

    public int NumberOfSpectators => 0;
    public int LocalPort => 0;
    public int NumberOfPlayers { get; private set; }

    public INetcodeRandom Random => random;

    public SessionMode Mode => SessionMode.Replay;
    public void DisconnectPlayer(NetcodePlayer player) { }
    public ResultCode AddLocalInput(NetcodePlayer player, in TInput localInput) => ResultCode.Ok;
    public IReadOnlySet<NetcodePlayer> GetPlayers() => fakePlayers;
    public IReadOnlySet<NetcodePlayer> GetSpectators() => FrozenSet<NetcodePlayer>.Empty;
    public NetcodePlayer? FindPlayer(Guid id) => playerMap.GetValueOrDefault(id);

    public ReadOnlySpan<SynchronizedInput<TInput>> CurrentSynchronizedInputs => syncInputBuffer;
    public ReadOnlySpan<TInput> CurrentInputs => inputBuffer;

    public void BeginFrame() { }

    public void AdvanceFrame()
    {
        if (ReplayController.IsPaused)
            return;

        if (ReplayController.IsBackward)
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

    public PlayerConnectionStatus GetPlayerStatus(NetcodePlayer player) => PlayerConnectionStatus.Connected;

    public ResultCode AddPlayer(NetcodePlayer player) => ResultCode.NotSupported;

    public bool UpdateNetworkStats(NetcodePlayer player)
    {
        player.NetworkStats.Session = this;
        player.NetworkStats.Valid = false;
        return false;
    }

    public void SetFrameDelay(NetcodePlayer player, int delayInFrames) { }

    public void Start(CancellationToken stoppingToken = default)
    {
        callbacks.OnSessionStart();
        isSynchronizing = false;
    }

    public Task WaitToStop(CancellationToken stoppingToken = default) => Task.CompletedTask;

    [MemberNotNull(nameof(callbacks))]
    public void SetHandler(INetcodeSessionHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        callbacks = handler;
    }

    public ResultCode SynchronizeInputs()
    {
        if (isSynchronizing || ReplayController.IsPaused)
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

        random.UpdateSeed(CurrentFrame, inputBuffer, extraSeedState);
        return ResultCode.Ok;
    }

    uint extraSeedState;
    public void SetRandomSeed(uint seed, uint extraState = 0) => extraSeedState = unchecked(seed + extraState);

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

    public bool LoadFrame(Frame frame)
    {
        frame = Frame.Max(in frame, in Frame.Zero);

        if (frame.Number == CurrentFrame.Number)
        {
            logger.Write(LogLevel.Trace, "Skipping NOP.");
            return true;
        }

        if (!stateStore.TryLoad(in frame, out var savedFrame))
        {
            ReplayController.IsBackward = false;
            ReplayController.Pause();
            return false;
        }

        logger.Write(LogLevel.Trace,
            $"Loading replay frame {savedFrame.Frame} (checksum: {savedFrame.Checksum:x8})");
        var offset = 0;
        BinaryBufferReader reader = new(savedFrame.GameState.WrittenSpan, ref offset, endianness);
        callbacks.LoadState(in frame, in reader);
        CurrentFrame = savedFrame.Frame;
        return true;
    }
}
