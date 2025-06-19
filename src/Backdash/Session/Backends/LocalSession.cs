using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
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
    readonly HashSet<NetcodePlayer> addedPlayers = new(Max.NumberOfPlayers);
    readonly Dictionary<Guid, NetcodePlayer> allPlayers = [];

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

    public async ValueTask DisposeAsync()
    {
        Dispose();
        await WaitToStop();
    }

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

    public IReadOnlySet<NetcodePlayer> GetPlayers() => addedPlayers;

    public IReadOnlySet<NetcodePlayer> GetSpectators() => FrozenSet<NetcodePlayer>.Empty;

    public NetcodePlayer? FindPlayer(Guid id) => allPlayers.GetValueOrDefault(id);
    public void DisconnectPlayer(NetcodePlayer player) { }

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

    public void WriteLog(LogLevel level, string message) => logger.Write(level, message);
    public void WriteLog(string message, Exception? error = null) => logger.Write(message, error);

    public ResultCode AddPlayer(NetcodePlayer player)
    {
        var result = player.Type switch
        {
            PlayerType.Local => AddLocalPlayer(player),
            PlayerType.Spectator or PlayerType.Remote => ResultCode.NotSupported,
            _ => throw new ArgumentOutOfRangeException(nameof(player)),
        };

        return result;
    }

    public ResultCode AddLocalPlayer(NetcodePlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);
        if (!player.IsLocal())
            return ResultCode.InvalidNetcodePlayer;

        if (addedPlayers.Count >= Max.NumberOfPlayers)
            return ResultCode.TooManyPlayers;

        player.SetQueue(addedPlayers.Count);

        if (!allPlayers.TryAdd(player.Id, player) || !addedPlayers.Add(player))
        {
            player.SetQueue(-1);
            return ResultCode.DuplicatedPlayer;
        }

        IncrementInputBufferSize();

        inputQueues[player.Index] = new(player.Index, options.InputQueueLength, logger, comparer)
        {
            LocalFrameDelay = options.InputDelayFrames,
        };
        return ResultCode.Ok;
    }

    void IncrementInputBufferSize()
    {
        var newSize = syncInputBuffer.Length + 1;
        Array.Resize(ref syncInputBuffer, newSize);
        Array.Resize(ref inputBuffer, newSize);
        Array.Resize(ref inputQueues, newSize);
    }

    public PlayerConnectionStatus GetPlayerStatus(NetcodePlayer player) =>
        addedPlayers.Contains(player) ? PlayerConnectionStatus.Local : PlayerConnectionStatus.Unknown;

    public bool UpdateNetworkStats(NetcodePlayer player)
    {
        var info = player.NetworkStats;
        info.Valid = false;
        return false;
    }

    public ResultCode AddLocalInput(NetcodePlayer player, in TInput localInput)
    {
        if (!running)
            return ResultCode.NotSynchronized;

        if (player.Type is not PlayerType.Local)
            return ResultCode.InvalidNetcodePlayer;

        if (!IsPlayerKnown(player))
            return ResultCode.PlayerOutOfRange;

        GameInput<TInput> gameInput = new(localInput, CurrentFrame);
        inputQueues[player.Index].AddInput(ref gameInput);

        return ResultCode.Ok;
    }

    bool IsPlayerKnown(NetcodePlayer player) =>
        player.Index >= 0 && addedPlayers.Contains(player);

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
        if (frame.Number < 1) return false;

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

    public void SetFrameDelay(NetcodePlayer player, int delayInFrames)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(delayInFrames);
        ThrowIf.ArgumentOutOfBounds(player.Index, 0, addedPlayers.Count);

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
