using System.Buffers;
using Backdash.Core;
using Backdash.Data;
using Backdash.Network;
using Backdash.Serialization.Buffer;
using Backdash.Synchronizing.Input;
using Backdash.Synchronizing.Random;
using Backdash.Synchronizing.State;

namespace Backdash.Backends;

sealed class SyncTestBackend<TInput> : IRollbackSession<TInput>
    where TInput : unmanaged
{
    readonly record struct SavedFrameBytes(
        Frame Frame,
        int Checksum,
        byte[] State,
        GameInput<TInput> Input
    );

    readonly Synchronizer<TInput> synchronizer;
    readonly TaskCompletionSource tsc = new();
    readonly Logger logger;
    readonly IDeterministicRandom deterministicRandom;
    readonly HashSet<PlayerHandle> addedPlayers = [];
    readonly HashSet<PlayerHandle> addedSpectators = [];
    readonly Queue<SavedFrameBytes> savedFrames = [];
    readonly SynchronizedInput<TInput>[] syncInputBuffer = new SynchronizedInput<TInput>[Max.NumberOfPlayers];
    readonly TInput[] inputBuffer = new TInput[Max.NumberOfPlayers];
    readonly RollbackOptions options;
    readonly FrameSpan checkDistance;
    readonly bool throwError;
    readonly IStateDesyncHandler? mismatchHandler;
    readonly IInputGenerator<TInput>? inputGenerator;

    IRollbackHandler callbacks;
    bool inRollback;
    bool running;
    Task backGroundJobTask = Task.CompletedTask;
    GameInput<TInput> currentInput;
    GameInput<TInput> lastInput;
    Frame lastVerified = Frame.Zero;

    public SyncTestBackend(RollbackOptions options,
        FrameSpan checkDistance,
        bool throwError,
        IStateDesyncHandler? mismatchHandler,
        BackendServices<TInput> services)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        ThrowHelpers.ThrowIfTypeTooBigForStack<TInput>();
        ThrowHelpers.ThrowIfTypeTooBigForStack<GameInput<TInput>>();
        ThrowHelpers.ThrowIfArgumentIsNegative(checkDistance.FrameCount);
        this.options = options;
        this.checkDistance = checkDistance;
        this.throwError = throwError;
        this.mismatchHandler = mismatchHandler;
        deterministicRandom = services.DeterministicRandom;
        inputGenerator = services.InputGenerator;
        logger = services.Logger;
        callbacks ??= new EmptySessionHandler(logger);
        synchronizer = new(
            options, logger,
            addedPlayers,
            services.StateStore,
            services.ChecksumProvider,
            new(Max.NumberOfPlayers)
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
    public IDeterministicRandom Random => deterministicRandom;
    public Frame CurrentFrame => synchronizer.CurrentFrame;
    public SessionMode Mode => SessionMode.SyncTest;
    public FrameSpan FramesBehind => synchronizer.FramesBehind;
    public FrameSpan RollbackFrames => synchronizer.RollbackFrames;

    public IReadOnlyCollection<PlayerHandle> GetPlayers() =>
        addedPlayers.Count is 0 ? [new(PlayerType.Local, 1, 0)] : addedPlayers;

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
        tsc.SetCanceled(stoppingToken);
        await backGroundJobTask.WaitAsync(stoppingToken).ConfigureAwait(false);
    }

    public ResultCode AddPlayer(Player player)
    {
        if (addedPlayers.Count >= Max.NumberOfPlayers)
            return ResultCode.TooManyPlayers;

        if (!addedPlayers.Add(player.Handle))
            return ResultCode.DuplicatedPlayer;

        if (player.IsSpectator())
        {
            if (!addedSpectators.Add(player.Handle))
                return ResultCode.DuplicatedPlayer;
        }
        else
        {
            if (!addedPlayers.Add(player.Handle))
                return ResultCode.DuplicatedPlayer;
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

    public bool GetNetworkStatus(in PlayerHandle player, ref PeerNetworkStats info) => false;

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

        inputBuffer[0] = lastInput.Data;
        syncInputBuffer[0] = new(lastInput.Data, false);

        var inputPopCount = options.UseInputSeedForRandom ? Mem.PopCount<TInput>(inputBuffer.AsSpan()) : 0;
        deterministicRandom.UpdateSeed(CurrentFrame.Number, inputPopCount);

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
        var lastSaved = synchronizer.GetLastSavedFrame();
        var stateBytes = ArrayPool<byte>.Shared.Rent(lastSaved.GameState.WrittenCount);
        lastSaved.GameState.WrittenSpan.CopyTo(stateBytes);

        var frame = synchronizer.CurrentFrame;
        savedFrames.Enqueue(new(
            Frame: frame,
            Input: lastInput,
            State: stateBytes,
            Checksum: lastSaved.Checksum
        ));
        if (frame - lastVerified != checkDistance.FrameValue)
            return;
        // We've gone far enough ahead and should now start replaying frames.
        // Load the last verified frame and set the rollback flag to true.
        synchronizer.LoadFrame(in lastVerified);
        inRollback = true;
        while (savedFrames.Count > 0)
        {
            callbacks.AdvanceFrame();
            // Verify that the checksum of this frame is the same as the one in our list.
            var info = savedFrames.Dequeue();
            ArrayPool<byte>.Shared.Return(info.State);
            if (info.Frame != synchronizer.CurrentFrame)
            {
                var message = $"Frame number {info.Frame.Number} does not match saved frame number {frame}";
                logger.Write(LogLevel.Error, message);
                if (throwError) throw new NetcodeException(message);
            }

            var last = synchronizer.GetLastSavedFrame();
            if (info.Checksum != last.Checksum)
                HandleDesync(frame, info, last);
            else
                logger.Write(LogLevel.Trace, $"Checksum #{last.Checksum:x8} for frame {info.Frame.Number} matches");
        }

        lastVerified = frame;
        inRollback = false;
    }

    void HandleDesync(Frame frame, SavedFrameBytes current, SavedFrame previous)
    {
        const LogLevel level = LogLevel.Error;
        var message = $"Checksum for frame {frame} does NOT match: (#{previous.Checksum:x8} != #{current.Checksum:x8})\n";
        logger.Write(LogLevel.Error, message);

        var (currentOffset, lastOffset) = (0, 0);

        BinaryBufferReader currentReader = new(current.State, ref currentOffset);
        var currentBody = callbacks.GetStateString(current.Frame, in currentReader);

        BinaryBufferReader previousReader = new(previous.GameState.WrittenSpan, ref lastOffset);
        var previousBody = callbacks.GetStateString(current.Frame, in previousReader);

        LogSaveState(level, "CURRENT", currentBody, current.Checksum, current.Frame, current.Input.Frame.Number);
        LogSaveState(level, "LAST", previousBody, previous.Checksum, previous.Frame);

        if (mismatchHandler is not null)
        {
            (currentOffset, lastOffset) = (0, 0);
            mismatchHandler.Handle(currentBody, current.Checksum, previousBody, previous.Checksum);
            mismatchHandler.Handle(in currentReader, current.Checksum, in previousReader, previous.Checksum);
        }

        if (throwError) throw new NetcodeException(message);
    }

    void LogSaveState(LogLevel level,
        string description, string body,
        int checksum, Frame frame,
        object? extra = null
    )
    {
        logger.Write(level, $"=> SAVED [{description}] (Frame {frame}{(extra is not null ? $" / {extra}" : "")})");
        logger.Write(level, $"== START STATE #{checksum:x8} ==");
        LogText(level, body);
        logger.Write(level, $"== END STATE #{checksum:x8} ==\n");
    }

    void LogText(LogLevel level, string text)
    {
        if (level is LogLevel.None || string.IsNullOrEmpty(text)) return;

        var jsonChunks =
            text
                .Split(Environment.NewLine)
                .SelectMany(p => p
                    .Chunk(LogStringBuffer.Capacity / 2)
                    .Select(x => new string(x)));

        foreach (var chunk in jsonChunks)
            logger.Write(level, chunk);
    }

    public void SetFrameDelay(PlayerHandle player, int delayInFrames)
    {
        ThrowHelpers.ThrowIfArgumentOutOfBounds(player.InternalQueue, 0, addedPlayers.Count);
        ThrowHelpers.ThrowIfArgumentIsNegative(delayInFrames);
        synchronizer.SetFrameDelay(player, delayInFrames);
    }

    public void SetHandler(IRollbackHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        callbacks = handler;
        synchronizer.Callbacks = handler;
    }
}
