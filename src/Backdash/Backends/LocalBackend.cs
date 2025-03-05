using Backdash.Core;
using Backdash.Data;
using Backdash.Network;
using Backdash.Synchronizing.Input;
using Backdash.Synchronizing.Random;
using Backdash.Synchronizing.State;

namespace Backdash.Backends;

sealed class LocalBackend<TInput> : INetcodeSession<TInput> where TInput : unmanaged
{
    readonly Synchronizer<TInput> synchronizer;
    readonly TaskCompletionSource tsc = new();
    readonly Logger logger;
    readonly HashSet<PlayerHandle> addedPlayers = new(Max.NumberOfPlayers);

    SynchronizedInput<TInput>[] syncInputBuffer = [];
    TInput[] inputBuffer = [];

    readonly NetcodeOptions options;

    bool running;

    INetcodeSessionHandler callbacks;
    Task backGroundJobTask = Task.CompletedTask;

    public LocalBackend(NetcodeOptions options, BackendServices<TInput> services)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);

        this.options = options;
        Random = services.DeterministicRandom;
        logger = services.Logger;
        callbacks ??= new EmptySessionHandler(logger);
        synchronizer = new(
            options, logger,
            addedPlayers,
            services.StateStore,
            services.ChecksumProvider,
            new(Max.NumberOfPlayers),
            services.InputComparer
        )
        {
            Callbacks = callbacks,
        };
    }

    public void Dispose() => tsc.SetResult();
    public int NumberOfPlayers => Math.Max(addedPlayers.Count, 1);
    public int NumberOfSpectators => 0;

    public IDeterministicRandom Random { get; }

    public Frame CurrentFrame => synchronizer.CurrentFrame;
    public SessionMode Mode => SessionMode.Local;
    public FrameSpan FramesBehind => synchronizer.FramesBehind;
    public FrameSpan RollbackFrames => synchronizer.RollbackFrames;
    public SavedFrame CurrentSavedFrame => synchronizer.GetLastSavedFrame();

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
        synchronizer.AddQueue(player.Handle);

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

        GameInput<TInput> input = new()
        {
            Data = localInput,
        };

        if (player.Type is not PlayerType.Local)
            return ResultCode.InvalidPlayerHandle;

        if (!IsPlayerKnown(in player))
            return ResultCode.PlayerOutOfRange;

        if (synchronizer.InRollback)
            return ResultCode.InRollback;

        if (!synchronizer.AddLocalInput(in player, ref input))
            return ResultCode.PredictionThreshold;

        return ResultCode.Ok;
    }

    bool IsPlayerKnown(in PlayerHandle player) =>
        player.InternalQueue >= 0 && addedPlayers.Contains(player);

    public void BeginFrame()
    {
        var currentFrame = synchronizer.CurrentFrame;
        logger.Write(LogLevel.Trace, $"Beginning of frame({currentFrame})...");
        synchronizer.SetLastConfirmedFrame(currentFrame);
    }

    public ResultCode SynchronizeInputs()
    {
        synchronizer.SynchronizeInputs(syncInputBuffer, inputBuffer);

        var inputPopCount = options.UseInputSeedForRandom ? Mem.PopCount<TInput>(inputBuffer.AsSpan()) : 0;
        Random.UpdateSeed(CurrentFrame.Number, inputPopCount);

        return ResultCode.Ok;
    }

    public ref readonly SynchronizedInput<TInput> GetInput(in PlayerHandle player) =>
        ref syncInputBuffer[player.InternalQueue];

    public ref readonly SynchronizedInput<TInput> GetInput(int index) =>
        ref syncInputBuffer[index];

    public void AdvanceFrame()
    {
        logger.Write(LogLevel.Trace, $"End of frame({synchronizer.CurrentFrame})...");
        synchronizer.IncrementFrame();
    }


    public void SetFrameDelay(PlayerHandle player, int delayInFrames)
    {
        ThrowIf.ArgumentOutOfBounds(player.InternalQueue, 0, addedPlayers.Count);
        ArgumentOutOfRangeException.ThrowIfNegative(delayInFrames);
        synchronizer.SetFrameDelay(player, delayInFrames);
    }

    public void SetHandler(INetcodeSessionHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        callbacks = handler;
        synchronizer.Callbacks = handler;
    }
}
