using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Backdash.Core;
using Backdash.Network;
using Backdash.Options;
using Backdash.Serialization;
using Backdash.Synchronizing.Input;
using Backdash.Synchronizing.Random;
using Backdash.Synchronizing.State;

namespace Backdash.Backends;

sealed class LocalSession<TInput> : INetcodeSession<TInput> where TInput : unmanaged
{
    readonly TaskCompletionSource tsc = new();
    readonly Logger logger;
    readonly HashSet<PlayerHandle> addedPlayers = new(Max.NumberOfPlayers);

    InputQueue<TInput>[] inputQueues = [];
    SynchronizedInput<TInput>[] syncInputBuffer = [];
    TInput[] inputBuffer = [];

    readonly IStateStore stateStore;
    readonly IChecksumProvider checksumProvider;
    readonly IDeterministicRandom<TInput> random;
    readonly Endianness endianness;
    readonly EqualityComparer<TInput> comparer;
    readonly NetcodeOptions options;

    bool running;

    INetcodeSessionHandler callbacks;
    Task backGroundJobTask = Task.CompletedTask;

    public LocalSession(
        NetcodeOptions options,
        SessionServices<TInput> services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        stateStore = services.StateStore;
        checksumProvider = services.ChecksumProvider;
        random = services.DeterministicRandom;
        logger = services.Logger;
        endianness = options.GetStateSerializationEndianness();
        callbacks = services.SessionHandler;
        comparer = services.InputComparer;
        stateStore.Initialize(options.TotalSavedFramesAllowed);
        this.options = options;
    }

    public void Dispose() => tsc.SetResult();
    public int NumberOfPlayers => Math.Max(addedPlayers.Count, 1);
    public int NumberOfSpectators => 0;
    public int FixedFrameRate => options.FrameRate;
    public int LocalPort => 0;
    public INetcodeRandom Random => random;

    public ReadOnlySpan<SynchronizedInput<TInput>> CurrentSynchronizedInputs => syncInputBuffer;
    public ReadOnlySpan<TInput> CurrentInputs => inputBuffer;

    public Frame CurrentFrame { get; private set; } = Frame.Zero;
    public SessionMode Mode => SessionMode.Local;
    public FrameSpan FramesBehind => FrameSpan.Zero;
    public FrameSpan RollbackFrames => FrameSpan.Zero;
    public bool IsInRollback => false;
    public SavedFrame GetCurrentSavedFrame() => stateStore.Last();

    public IReadOnlySet<PlayerHandle> GetPlayers() => addedPlayers;

    public IReadOnlySet<PlayerHandle> GetSpectators() => FrozenSet<PlayerHandle>.Empty;
    public void DisconnectPlayer(in PlayerHandle player) { }

    public void Start(CancellationToken stoppingToken = default)
    {
        if (running) return;
        running = true;
        callbacks.OnSessionStart();
        backGroundJobTask = tsc.Task.WaitAsync(stoppingToken);
    }

    public async Task WaitToStop(CancellationToken stoppingToken = default)
    {
        // ReSharper disable once MethodSupportsCancellation
        tsc.SetCanceled(stoppingToken);
        await backGroundJobTask.WaitAsync(stoppingToken).ConfigureAwait(false);
    }

    public ResultCode AddLocalPlayer(out PlayerHandle handle)
    {
        handle = default;

        if (addedPlayers.Count >= Max.NumberOfPlayers)
            return ResultCode.TooManyPlayers;

        PlayerHandle playerHandle = new(PlayerType.Local, addedPlayers.Count);

        if (!addedPlayers.Add(playerHandle))
            return ResultCode.DuplicatedPlayer;

        handle = playerHandle;
        IncrementInputBufferSize();

        inputQueues[handle.QueueIndex] = new(handle.QueueIndex, options.InputQueueLength, logger, comparer)
        {
            LocalFrameDelay = options.InputDelayFrames,
        };
        return ResultCode.Ok;
    }

    public ResultCode AddRemotePlayer(IPEndPoint endpoint, out PlayerHandle handle)
    {
        handle = default;
        return ResultCode.NotSupported;
    }

#pragma warning disable S4144
    public ResultCode AddSpectator(IPEndPoint endpoint, out PlayerHandle handle)
#pragma warning restore S4144
    {
        handle = default;
        return ResultCode.NotSupported;
    }

    void IncrementInputBufferSize()
    {
        var newSize = syncInputBuffer.Length + 1;
        Array.Resize(ref syncInputBuffer, newSize);
        Array.Resize(ref inputBuffer, newSize);
        Array.Resize(ref inputQueues, newSize);
    }

    public PlayerConnectionStatus GetPlayerStatus(in PlayerHandle player) =>
        addedPlayers.Contains(player) ? PlayerConnectionStatus.Local : PlayerConnectionStatus.Unknown;

    public bool GetNetworkStatus(in PlayerHandle player, ref PeerNetworkStats info)
    {
        info.RollbackFrames = RollbackFrames;
        info.CurrentFrame = CurrentFrame;
        info.Valid = false;
        return false;
    }

    public ResultCode AddLocalInput(in PlayerHandle player, in TInput localInput)
    {
        if (!running)
            return ResultCode.NotSynchronized;

        if (player.Type is not PlayerType.Local)
            return ResultCode.InvalidPlayerHandle;

        if (!IsPlayerKnown(in player))
            return ResultCode.PlayerOutOfRange;

        GameInput<TInput> gameInput = new(localInput, CurrentFrame);
        inputQueues[player.QueueIndex].AddInput(ref gameInput);

        return ResultCode.Ok;
    }

    bool IsPlayerKnown(in PlayerHandle player) =>
        player.QueueIndex >= 0 && addedPlayers.Contains(player);

    public void BeginFrame() => logger.Write(LogLevel.Trace, $"Beginning of frame({CurrentFrame.Number})");

    public ResultCode SynchronizeInputs()
    {
        for (var i = 0; i < inputQueues.Length; i++)
        {
            inputQueues[i].GetInput(CurrentFrame, out var input);
            inputBuffer[i] = input.Data;
            syncInputBuffer[i] = new(input.Data, false);
        }

        random.UpdateSeed(CurrentFrame, inputBuffer, extraSeedState);
        return ResultCode.Ok;
    }

    uint extraSeedState;
    public void SetRandomSeed(uint seed, uint extraState = 0) => extraSeedState = unchecked(seed + extraState);

    public void AdvanceFrame()
    {
        CurrentFrame++;
        SaveCurrentFrame();
        Array.Clear(inputBuffer);
        Array.Clear(syncInputBuffer);
        logger.Write(LogLevel.Trace, $"End of frame({CurrentFrame.Number})");
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
            return false;

        var offset = 0;
        BinaryBufferReader reader = new(savedFrame.GameState.WrittenSpan, ref offset, endianness);
        callbacks.LoadState(in frame, in reader);
        CurrentFrame = frame;

        var prevFrame = frame.Previous();
        ref var current = ref MemoryMarshal.GetReference(inputQueues.AsSpan());
        ref var limit = ref Unsafe.Add(ref current, inputQueues.Length);
        while (Unsafe.IsAddressLessThan(ref current, ref limit))
        {
            current.DiscardInputsAfter(in prevFrame);
            current = ref Unsafe.Add(ref current, 1)!;
        }

        return true;
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

    public void SetFrameDelay(PlayerHandle player, int delayInFrames)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(delayInFrames);
        ThrowIf.ArgumentOutOfBounds(player.QueueIndex, 0, addedPlayers.Count);

        ref var current = ref MemoryMarshal.GetReference(inputQueues.AsSpan());
        ref var limit = ref Unsafe.Add(ref current, inputQueues.Length);
        while (Unsafe.IsAddressLessThan(ref current, ref limit))
        {
            current.LocalFrameDelay = delayInFrames;
            current = ref Unsafe.Add(ref current, 1)!;
        }
    }

    [MemberNotNull(nameof(callbacks))]
    public void SetHandler(INetcodeSessionHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        callbacks = handler;
    }
}
