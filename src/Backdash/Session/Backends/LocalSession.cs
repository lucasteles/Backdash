using Backdash.Core;
using Backdash.Data;
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
        callbacks ??= new EmptySessionHandler(logger);
        endianness = options.GetStateSerializationEndianness();
        stateStore.Initialize(options.TotalPredictionFrames);
    }

    public void Dispose() => tsc.SetResult();
    public int NumberOfPlayers => Math.Max(addedPlayers.Count, 1);
    public int NumberOfSpectators => 0;
    public int LocalPort => 0;
    public INetcodeRandom Random => random;

    public Frame CurrentFrame { get; private set; } = Frame.Zero;
    public SessionMode Mode => SessionMode.Local;
    public FrameSpan FramesBehind => FrameSpan.Zero;
    public FrameSpan RollbackFrames => FrameSpan.Zero;
    public SavedFrame GetCurrentSavedFrame() => stateStore.Last();

    public IReadOnlyCollection<PlayerHandle> GetPlayers() => addedPlayers;

    public IReadOnlyCollection<PlayerHandle> GetSpectators() => [];
    public void DisconnectPlayer(in PlayerHandle player) { }

    public void Start(CancellationToken stoppingToken = default)
    {
        if (!running)
        {
            callbacks.OnSessionStart();
            running = true;
        }

        backGroundJobTask = tsc.Task.WaitAsync(stoppingToken);
    }

    public async Task WaitToStop(CancellationToken stoppingToken = default)
    {
        // ReSharper disable once MethodSupportsCancellation
        tsc.SetCanceled(stoppingToken);
        await backGroundJobTask.WaitAsync(stoppingToken).ConfigureAwait(false);
    }

    public ResultCode AddPlayer(Player player)
    {
        if (addedPlayers.Count >= Max.NumberOfPlayers)
            return ResultCode.TooManyPlayers;

        if (!player.IsLocal())
            return ResultCode.NotSupported;

        PlayerHandle handle = new(player.Handle.Type, player.Handle.Number, addedPlayers.Count);

        if (!addedPlayers.Add(handle))
            return ResultCode.DuplicatedPlayer;

        player.Handle = handle;
        IncrementInputBufferSize();

        return ResultCode.Ok;
    }

    void IncrementInputBufferSize()
    {
        Array.Resize(ref syncInputBuffer, syncInputBuffer.Length + 1);
        Array.Resize(ref inputBuffer, syncInputBuffer.Length);
    }

    public IReadOnlyList<ResultCode> AddPlayers(IReadOnlyList<Player> players)
    {
        var result = new ResultCode[players.Count];
        for (var index = 0; index < players.Count; index++)
            result[index] = AddPlayer(players[index]);
        return result;
    }

    public PlayerConnectionStatus GetPlayerStatus(in PlayerHandle player) =>
        addedPlayers.Contains(player) ? PlayerConnectionStatus.Local : PlayerConnectionStatus.Unknown;

    public bool GetNetworkStatus(in PlayerHandle player, ref PeerNetworkStats info) => false;

    public ResultCode AddLocalInput(PlayerHandle player, in TInput localInput)
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
        player.InternalQueue >= 0 && addedPlayers.Contains(player);

    public void BeginFrame() => logger.Write(LogLevel.Trace, $"Beginning of frame({CurrentFrame.Number})");

    public ResultCode SynchronizeInputs()
    {
        random.UpdateSeed(CurrentFrame, inputBuffer);
        return ResultCode.Ok;
    }

    public ref readonly SynchronizedInput<TInput> GetInput(in PlayerHandle player) =>
        ref syncInputBuffer[player.InternalQueue];

    public ref readonly SynchronizedInput<TInput> GetInput(int index) =>
        ref syncInputBuffer[index];

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
        ThrowIf.ArgumentOutOfBounds(player.InternalQueue, 0, addedPlayers.Count);
        ArgumentOutOfRangeException.ThrowIfNegative(delayInFrames);
    }

    public void SetHandler(INetcodeSessionHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        callbacks = handler;
    }
}
