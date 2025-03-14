using System.Buffers;
using Backdash.Core;
using Backdash.Data;
using Backdash.Network;
using Backdash.Options;
using Backdash.Serialization;
using Backdash.Synchronizing.Input;
using Backdash.Synchronizing.Random;
using Backdash.Synchronizing.State;

namespace Backdash.Backends;

sealed class SyncTestSession<TInput> : INetcodeSession<TInput>
    where TInput : unmanaged
{
    readonly record struct SavedFrameBytes(
        Frame Frame,
        uint Checksum,
        byte[] State,
        GameInput<TInput> Input
    );

    readonly Synchronizer<TInput> synchronizer;
    readonly TaskCompletionSource tsc = new();
    readonly Logger logger;
    readonly HashSet<PlayerHandle> addedPlayers = [];
    readonly HashSet<PlayerHandle> addedSpectators = [];
    readonly Queue<SavedFrameBytes> savedFrames = [];
    readonly SynchronizedInput<TInput>[] syncInputBuffer = new SynchronizedInput<TInput>[Max.NumberOfPlayers];
    readonly TInput[] inputBuffer = new TInput[Max.NumberOfPlayers];
    readonly FrameSpan checkDistance;
    readonly bool throwError;
    readonly IStateDesyncHandler? mismatchHandler;
    readonly IStateStringParser stateParser;
    readonly IInputProvider<TInput>? inputGenerator;
    readonly IDeterministicRandom<TInput> random;

    INetcodeSessionHandler callbacks;
    bool inRollback;
    bool running;
    Task backGroundJobTask = Task.CompletedTask;
    GameInput<TInput> currentInput;
    GameInput<TInput> lastInput;
    Frame lastVerified = Frame.Zero;

    public SyncTestSession(
        SyncTestOptions<TInput> syncTestOptions,
        NetcodeOptions options,
        SessionServices<TInput> services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(syncTestOptions);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(syncTestOptions.CheckDistance);

        checkDistance = new(syncTestOptions.CheckDistance);
        throwError = syncTestOptions.ThrowOnDesync;
        logger = services.Logger;
        random = services.DeterministicRandom;
        inputGenerator = syncTestOptions.InputProvider;
        mismatchHandler = syncTestOptions.DesyncHandler;
        callbacks ??= new EmptySessionHandler(logger);
        stateParser = syncTestOptions.StateStringParser ?? new HexStateStringParser();

        if (stateParser is JsonStateStringParser jsonParser)
            jsonParser.Logger = logger;

        synchronizer = new(
            options, logger,
            addedPlayers,
            services.StateStore,
            services.ChecksumProvider,
            new(options.NumberOfPlayers),
            services.InputComparer
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
    public int LocalPort => 0;
    public INetcodeRandom Random => random;
    public Frame CurrentFrame => synchronizer.CurrentFrame;
    public SessionMode Mode => SessionMode.SyncTest;
    public FrameSpan FramesBehind => synchronizer.FramesBehind;
    public FrameSpan RollbackFrames => synchronizer.RollbackFrames;

    public ReadOnlySpan<SynchronizedInput<TInput>> GetSynchronizedInputs() => syncInputBuffer;
    public ReadOnlySpan<TInput> GetInputs() => inputBuffer;

    public SavedFrame GetCurrentSavedFrame() => synchronizer.GetLastSavedFrame();

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

    public ResultCode AddLocalInput(PlayerHandle player, in TInput localInput)
    {
        if (!running)
            return ResultCode.NotSynchronized;

        var testInput = localInput;

        if (inputGenerator is not null)
            testInput = inputGenerator.Next();

        currentInput.Frame = synchronizer.CurrentFrame;
        currentInput.Data = testInput;
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
        random.UpdateSeed(CurrentFrame, inputBuffer);

        return ResultCode.Ok;
    }

    public ref readonly SynchronizedInput<TInput> GetInput(in PlayerHandle player) =>
        ref syncInputBuffer[player.InternalQueue];

    public ref readonly SynchronizedInput<TInput> GetInput(int index) =>
        ref syncInputBuffer[index];

    public bool LoadFrame(in Frame frame)
    {
        if (frame.IsNull || frame == CurrentFrame)
        {
            logger.Write(LogLevel.Trace, "Skipping NOP.");
            return true;
        }

        try
        {
            synchronizer.LoadFrame(in frame);
            return true;
        }
        catch (NetcodeException)
        {
            return false;
        }
    }

    public void AdvanceFrame()
    {
        logger.Write(LogLevel.Trace, $"End of frame({synchronizer.CurrentFrame})...");
        synchronizer.IncrementFrame();
        currentInput.Erase();

        if (inRollback) return;

        // Hold onto the current frame in our queue of saved states.
        // We'll need the checksum later to verify that our replay of the same frame got the same results.
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
        var message =
            $"Checksum for frame {frame} does NOT match: (#{previous.Checksum:x8} != #{current.Checksum:x8})\n";
        logger.Write(LogLevel.Error, message);

        var (currentOffset, lastOffset) = (0, 0);

        var stateObject = callbacks.GetCurrentState();
        BinaryBufferReader currentReader = new(current.State, ref currentOffset);
        var currentBody = stateParser.GetStateString(current.Frame, in currentReader, stateObject);

        BinaryBufferReader previousReader = new(previous.GameState.WrittenSpan, ref lastOffset);
        var previousBody = stateParser.GetStateString(current.Frame, in previousReader, stateObject);

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
        uint checksum, Frame frame,
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
