using System.Text.Json;
using Backdash.Core;
using Backdash.Data;
using Backdash.Network;
using Backdash.Sync.Input;
using Backdash.Sync.State;
namespace Backdash.Backends;
sealed class SyncTestBackend<TInput, TGameState> : IRollbackSession<TInput, TGameState>
    where TInput : struct
    where TGameState : IEquatable<TGameState>, new()
{
    readonly record struct SavedFrame(
        Frame Frame,
        int Checksum,
        JsonElement State,
        GameInput<TInput> Input
    );
    readonly Synchronizer<TInput, TGameState> synchronizer;
    readonly TaskCompletionSource tsc = new();
    readonly Logger logger;
    readonly HashSet<PlayerHandle> addedPlayers = [];
    readonly HashSet<PlayerHandle> addedSpectators = [];
    readonly Queue<SavedFrame> savedFrames = [];
    readonly SynchronizedInput<TInput>[] syncInputBuffer = new SynchronizedInput<TInput>[Max.RemoteConnections];
    readonly FrameSpan checkDistance;
    readonly bool throwError;
    readonly IInputGenerator<TInput>? inputGenerator;
    readonly JsonSerializerOptions jsonOptions = new()
    {
        WriteIndented = false,
        IncludeFields = true,
    };
    IRollbackHandler<TGameState> callbacks;
    bool inRollback;
    bool running;
    Task backGroundJobTask = Task.CompletedTask;
    GameInput<TInput> currentInput;
    GameInput<TInput> lastInput;
    Frame lastVerified = Frame.Zero;
    public SyncTestBackend(
        RollbackOptions options,
        FrameSpan checkDistance,
        bool throwError,
        BackendServices<TInput, TGameState> services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        ThrowHelpers.ThrowIfTypeTooBigForStack<TInput>();
        ThrowHelpers.ThrowIfTypeTooBigForStack<GameInput<TInput>>();
        ThrowHelpers.ThrowIfArgumentIsNegative(checkDistance.FrameCount);
        this.checkDistance = checkDistance;
        this.throwError = throwError;
        inputGenerator = services.InputGenerator;
        logger = services.Logger;
        callbacks ??= new EmptySessionHandler<TGameState>(logger);
        synchronizer = new(
            options, logger,
            addedPlayers,
            services.StateStore,
            services.ChecksumProvider,
            new(Max.RemoteConnections)
        )
        {
            Callbacks = callbacks,
        };
        currentInput = new();
        lastInput = new();
    }
    public void Dispose() => tsc.SetResult();
    public int NumberOfPlayers => Math.Max(addedPlayers.Count, 1);
    public int NumberOfSpectators => addedSpectators.Count;
    public Frame CurrentFrame => synchronizer.CurrentFrame;
    public bool IsSpectating => false;
    public FrameSpan FramesBehind => synchronizer.FramesBehind;
    public FrameSpan RollbackFrames => synchronizer.RollbackFrames;
    public IReadOnlyCollection<PlayerHandle> GetPlayers() =>
        addedPlayers.Count is 0 ? [new PlayerHandle(PlayerType.Local, 1, 0)] : addedPlayers;
    public IReadOnlyCollection<PlayerHandle> GetSpectators() => addedSpectators;
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
        tsc.SetCanceled();
        await backGroundJobTask.WaitAsync(stoppingToken);
    }
    public ResultCode AddPlayer(Player player)
    {
        if (addedPlayers.Count >= Max.RemoteConnections)
            return ResultCode.TooManyPlayers;
        if (!addedPlayers.Add(player.Handle))
            return ResultCode.Duplicated;
        if (player.IsSpectator())
        {
            if (!addedSpectators.Add(player.Handle))
                return ResultCode.Duplicated;
        }
        else
        {
            if (!addedPlayers.Add(player.Handle))
                return ResultCode.Duplicated;
        }
        return ResultCode.Ok;
    }
    public IReadOnlyList<ResultCode> AddPlayers(IReadOnlyList<Player> players)
    {
        var result = new ResultCode[players.Count];
        for (var index = 0; index < players.Count; index++)
            result[index] = AddPlayer(players[index]);
        return result;
    }
    public PlayerConnectionStatus GetPlayerStatus(in PlayerHandle player)
    {
        if (addedPlayers.Contains(player) || addedSpectators.Contains(player))
            return player.IsLocal() ? PlayerConnectionStatus.Local : PlayerConnectionStatus.Connected;
        return PlayerConnectionStatus.Unknown;
    }
    public bool GetNetworkStatus(in PlayerHandle player, ref RollbackNetworkStatus info) => false;
    public ResultCode AddLocalInput(PlayerHandle player, TInput localInput)
    {
        if (!running)
            return ResultCode.NotSynchronized;
        if (inputGenerator is not null)
            localInput = inputGenerator.Generate();
        currentInput.Frame = synchronizer.CurrentFrame;
        currentInput.Data = localInput;
        return ResultCode.Ok;
    }
    public void BeginFrame() => logger.Write(LogLevel.Trace, $"Beginning of frame({synchronizer.CurrentFrame})...");
    public ResultCode SynchronizeInputs()
    {
        if (inRollback)
        {
            lastInput = savedFrames.Peek().Input;
        }
        else
        {
            if (synchronizer.CurrentFrame == Frame.Zero)
                synchronizer.SaveCurrentFrame();
            lastInput = currentInput;
        }
        syncInputBuffer[0] = new(lastInput.Data, false);
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
        currentInput.Erase();
        if (inRollback) return;
        // Hold onto the current frame in our queue of saved states.  We'll need
        // the checksum later to verify that our replay of the same frame got the
        // same results.
        ref readonly var lastSaved = ref synchronizer.GetLastSavedFrame();
        var frame = synchronizer.CurrentFrame;
        savedFrames.Enqueue(new(
            Frame: frame,
            Input: lastInput,
            State: JsonSerializer.SerializeToElement(lastSaved.GameState, jsonOptions),
            Checksum: lastSaved.Checksum
        ));
        if (frame - lastVerified != checkDistance.Value)
            return;
        // We've gone far enough ahead and should now start replaying frames.
        // Load the last verified frame and set the rollback flag to true.
        synchronizer.LoadFrame(in lastVerified);
        inRollback = true;
        while (savedFrames.Count > 0)
        {
            callbacks.AdvanceFrame();
            // Verify that the checksum of this frame is the same as the one in our
            // list.
            var info = savedFrames.Dequeue();
            if (info.Frame != synchronizer.CurrentFrame)
            {
                var message = $"Frame number {info.Frame} does not match saved frame number {frame}";
                logger.Write(LogLevel.Error, message);
                if (throwError) throw new NetcodeException(message);
            }
            ref readonly var last = ref synchronizer.GetLastSavedFrame();
            var checksum = last.Checksum;
            if (info.Checksum != checksum)
            {
                LogSaveState(info, "current");
                LogSaveState(last, "last");
                var message = $"Checksum for frame {frame} does not match saved ({checksum} != {info.Checksum})";
                logger.Write(LogLevel.Error, message);
                if (throwError) throw new NetcodeException(message);
            }
            logger.Write(LogLevel.Debug, $"Checksum {checksum} for frame {info.Frame} matches");
        }
        lastVerified = frame;
        inRollback = false;
    }
    void LogSaveState(SavedFrame info, string description)
    {
        const LogLevel level = LogLevel.Information;
        logger.Write(level, $"=== SAVED STATE [{description.ToUpper()}] ({info.Frame}) ===\n");
        logger.Write(level, $"INPUT FRAME {info.Input.Frame}:");
        logger.Write(level, JsonSerializer.Serialize(info.Input.Data, jsonOptions));
        logger.Write(level, $"GAME STATE #{info.Checksum}:");
        LogJson(level, info.State);
        logger.Write(level, "====================================");
    }
    void LogSaveState(SavedFrame<TGameState> info, string description)
    {
        const LogLevel level = LogLevel.Information;
        logger.Write(level, $"=== SAVED STATE [{description.ToUpper()}] ({info.Frame}) ===\n");
        logger.Write(level, $"GAME STATE #{info.Checksum}:");
        LogJson(level, info.GameState);
        logger.Write(level, "====================================");
    }
    void LogJson<TValue>(LogLevel level, TValue value)
    {
        var jsonChunks = JsonSerializer
            .Serialize(value, jsonOptions)
            .Chunk(LogStringBuffer.Capacity / 2)
            .Select(x => new string(x));
        foreach (var chunk in jsonChunks)
            logger.Write(level, chunk);
    }
    public void SetFrameDelay(PlayerHandle player, int delayInFrames)
    {
        ThrowHelpers.ThrowIfArgumentOutOfBounds(player.InternalQueue, 0, addedPlayers.Count);
        ThrowHelpers.ThrowIfArgumentIsNegative(delayInFrames);
        synchronizer.SetFrameDelay(player, delayInFrames);
    }
    public void SetHandler(IRollbackHandler<TGameState> handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        callbacks = handler;
        synchronizer.Callbacks = handler;
    }
}
