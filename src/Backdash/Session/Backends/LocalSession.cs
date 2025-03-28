using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using Backdash.Core;
using Backdash.Network;
using Backdash.Options;
using Backdash.Serialization;
using Backdash.Synchronizing.Random;
using Backdash.Synchronizing.State;

namespace Backdash.Backends;

sealed class LocalSession<TInput> : INetcodeSession<TInput> where TInput : unmanaged
{
    readonly TaskCompletionSource tsc = new();
    readonly Logger logger;
    readonly HashSet<PlayerHandle> addedPlayers = new(Max.NumberOfPlayers);

    SynchronizedInput<TInput>[] syncInputBuffer = [];
    TInput[] inputBuffer = [];

    readonly IStateStore stateStore;
    readonly IChecksumProvider checksumProvider;
    readonly IDeterministicRandom<TInput> random;
    readonly Endianness endianness;

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
        stateStore.Initialize(options.TotalPredictionFrames);
    }

    public void Dispose() => tsc.SetResult();
    public int NumberOfPlayers => Math.Max(addedPlayers.Count, 1);
    public int NumberOfSpectators => 0;
    public int LocalPort => 0;
    public INetcodeRandom Random => random;

    public ReadOnlySpan<SynchronizedInput<TInput>> CurrentSynchronizedInputs => syncInputBuffer;
    public ReadOnlySpan<TInput> CurrentInputs => inputBuffer;

    public Frame CurrentFrame { get; private set; } = Frame.Zero;
    public SessionMode Mode => SessionMode.Local;
    public FrameSpan FramesBehind => FrameSpan.Zero;
    public FrameSpan RollbackFrames => FrameSpan.Zero;
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
        Array.Resize(ref syncInputBuffer, syncInputBuffer.Length + 1);
        Array.Resize(ref inputBuffer, syncInputBuffer.Length);
    }

    public PlayerConnectionStatus GetPlayerStatus(in PlayerHandle player) =>
        addedPlayers.Contains(player) ? PlayerConnectionStatus.Local : PlayerConnectionStatus.Unknown;

    public bool GetNetworkStatus(in PlayerHandle player, ref PeerNetworkStats info) => false;

    public ResultCode AddLocalInput(in PlayerHandle player, in TInput localInput)
    {
        if (!running)
            return ResultCode.NotSynchronized;

        if (player.Type is not PlayerType.Local)
            return ResultCode.InvalidPlayerHandle;

        if (!IsPlayerKnown(in player))
            return ResultCode.PlayerOutOfRange;

        inputBuffer[player.Index] = localInput;
        syncInputBuffer[player.Index] = localInput;

        return ResultCode.Ok;
    }

    bool IsPlayerKnown(in PlayerHandle player) =>
        player.QueueIndex >= 0 && addedPlayers.Contains(player);

    public void BeginFrame() => logger.Write(LogLevel.Trace, $"Beginning of frame({CurrentFrame.Number})");

    public ResultCode SynchronizeInputs()
    {
        random.UpdateSeed(CurrentFrame, inputBuffer);
        return ResultCode.Ok;
    }

    public void AdvanceFrame()
    {
        CurrentFrame++;
        SaveCurrentFrame();
        Array.Clear(inputBuffer);
        Array.Clear(syncInputBuffer);
        logger.Write(LogLevel.Trace, $"End of frame({CurrentFrame.Number})");
    }

    public bool LoadFrame(in Frame frame)
    {
        if (frame.IsNull || frame == CurrentFrame)
        {
            logger.Write(LogLevel.Trace, "Skipping NOP.");
            return true;
        }

        try
        {
            var savedFrame =
                frame.Number < 0
                    ? stateStore.Load(Frame.Zero)
                    : stateStore.Load(in frame);

            var offset = 0;
            BinaryBufferReader reader = new(savedFrame.GameState.WrittenSpan, ref offset, endianness);
            callbacks.LoadState(in frame, in reader);
            CurrentFrame = frame;
            return true;
        }
        catch (NetcodeException)
        {
            return false;
        }
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
        ThrowIf.ArgumentOutOfBounds(player.QueueIndex, 0, addedPlayers.Count);
        ArgumentOutOfRangeException.ThrowIfNegative(delayInFrames);
    }

    [MemberNotNull(nameof(callbacks))]
    public void SetHandler(INetcodeSessionHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        callbacks = handler;
    }
}
