using System.Buffers;
using System.Collections.Frozen;
using System.Diagnostics.CodeAnalysis;
using Backdash.Core;
using Backdash.Network;
using Backdash.Options;
using Backdash.Serialization;
using Backdash.Synchronizing.Input;
using Backdash.Synchronizing.Input.Confirmed;
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
        int StateSize,
        GameInput<ConfirmedInputs<TInput>> Inputs
    )
    {
        public GameInput<TInput> GetInput(NetcodePlayer player) => new(Inputs.Data.Inputs[player.Index], Frame);
    };

    sealed class PlayerInputState
    {
        public GameInput<TInput> Current = new();
        public GameInput<TInput> Last = new();
    }

    readonly Synchronizer<TInput> synchronizer;
    readonly TaskCompletionSource tsc = new();
    readonly Logger logger;
    readonly HashSet<NetcodePlayer> addedSpectators = [];
    readonly Dictionary<NetcodePlayer, PlayerInputState> addedPlayers = [];
    readonly Dictionary<Guid, NetcodePlayer> allPlayers = [];
    readonly Queue<SavedFrameBytes> savedFrames = [];
    readonly SynchronizedInput<TInput>[] syncInputBuffer = new SynchronizedInput<TInput>[Max.NumberOfPlayers];
    readonly TInput[] inputBuffer = new TInput[Max.NumberOfPlayers];
    readonly FrameSpan checkDistance;
    readonly bool throwError;
    readonly IStateDesyncHandler? mismatchHandler;
    readonly IStateStringParser stateParser;
    readonly IInputProvider<TInput>? inputGenerator;
    readonly IDeterministicRandom<TInput> random;
    readonly Endianness endianness;
    readonly bool logStateOnDesync;

    INetcodeSessionHandler callbacks;
    bool inRollback;
    bool running;
    Task backGroundJobTask = Task.CompletedTask;
    Frame lastVerified = Frame.Zero;

    public int FixedFrameRate { get; }

    readonly IReadOnlySet<NetcodePlayer> localPlayerFallback = new HashSet<NetcodePlayer>
    {
        new(0, PlayerType.Local),
    }.ToFrozenSet();

    public SyncTestSession(
        SyncTestOptions<TInput> syncTestOptions,
        NetcodeOptions options,
        SessionServices<TInput> services
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(syncTestOptions);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(syncTestOptions.CheckDistanceFrames);

        FixedFrameRate = options.FrameRate;
        checkDistance = new(syncTestOptions.CheckDistanceFrames);
        logStateOnDesync = syncTestOptions.LogStateOnDesync;
        throwError = syncTestOptions.ThrowOnDesync;
        logger = services.Logger;
        random = services.DeterministicRandom;
        inputGenerator = syncTestOptions.InputProvider;
        mismatchHandler = syncTestOptions.DesyncHandler;
        callbacks = services.SessionHandler;

        stateParser = syncTestOptions.StateStringParser ?? new DefaultStateStringParser();
        if (stateParser is JsonStateStringParser jsonParser)
            jsonParser.Logger = logger;

        synchronizer = new(
            options, logger,
            addedPlayers.Keys,
            services.StateStore,
            services.ChecksumProvider,
            new(options.NumberOfPlayers),
            services.InputComparer
        )
        {
            Callbacks = callbacks,
        };

        endianness = options.GetStateSerializationEndianness();
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

    public ReadOnlySpan<SynchronizedInput<TInput>> CurrentSynchronizedInputs => syncInputBuffer;

    public ReadOnlySpan<TInput> CurrentInputs => inputBuffer;

    public bool IsInRollback => synchronizer.InRollback;
    public SavedFrame GetCurrentSavedFrame() => synchronizer.GetLastSavedFrame();

    public IReadOnlySet<NetcodePlayer> GetPlayers() =>
        addedPlayers.Count is 0 ? localPlayerFallback : addedPlayers.Keys.ToHashSet();

    public IReadOnlySet<NetcodePlayer> GetSpectators() => addedSpectators;
    public NetcodePlayer? FindPlayer(Guid id) => allPlayers.GetValueOrDefault(id);
    public void DisconnectPlayer(NetcodePlayer player) { }

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

    public void WriteLog(LogLevel level, string message) => logger.Write(level, message);
    public void WriteLog(string message, Exception? error = null) => logger.Write(message, error);

    public ResultCode AddPlayer(NetcodePlayer player)
    {
        var result = player.Type switch
        {
            PlayerType.Local => AddLocalPlayer(player),
            PlayerType.Spectator => AddSpectator(player),
            PlayerType.Remote => ResultCode.NotSupported,
            _ => throw new ArgumentOutOfRangeException(nameof(player)),
        };

        return result;
    }

    public ResultCode AddLocalPlayer(NetcodePlayer player)
    {
        if (!player.IsLocal())
            return ResultCode.InvalidNetcodePlayer;

        if (addedPlayers.Count >= Max.NumberOfPlayers)
            return ResultCode.TooManyPlayers;

        player.SetQueue(addedPlayers.Count);
        if (!addedPlayers.TryAdd(player, new()) || !allPlayers.TryAdd(player.Id, player))
        {
            player.SetQueue(-1);
            return ResultCode.DuplicatedPlayer;
        }

        return ResultCode.Ok;
    }

    public ResultCode AddSpectator(NetcodePlayer player)
    {
        if (!player.IsSpectator())
            return ResultCode.InvalidNetcodePlayer;

        if (addedSpectators.Count >= Max.NumberOfSpectators)
            return ResultCode.TooManyPlayers;

        player.SetQueue(addedSpectators.Count);
        if (!allPlayers.TryAdd(player.Id, player) || !addedSpectators.Add(player))
        {
            player.SetQueue(-1);
            return ResultCode.DuplicatedPlayer;
        }

        return ResultCode.Ok;
    }

    public PlayerConnectionStatus GetPlayerStatus(NetcodePlayer player)
    {
        if (addedPlayers.ContainsKey(player) || addedSpectators.Contains(player))
            return player.IsLocal() ? PlayerConnectionStatus.Local : PlayerConnectionStatus.Connected;
        return PlayerConnectionStatus.Unknown;
    }

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

        if (!addedPlayers.TryGetValue(player, out var playerInput))
            return ResultCode.InvalidNetcodePlayer;

        var testInput = localInput;

        if (inputGenerator is not null)
            testInput = inputGenerator.Next();

        playerInput.Current.Frame = synchronizer.CurrentFrame;
        playerInput.Current.Data = testInput;
        return ResultCode.Ok;
    }

    public void BeginFrame()
    {
        logger.Write(LogLevel.Trace, $"Beginning of frame ({synchronizer.CurrentFrame.Number})...");
        synchronizer.UpdateRollbackFrameCounter();
    }

    public ResultCode SynchronizeInputs()
    {
        if (synchronizer.CurrentFrame.Number is 0 && !inRollback)
            synchronizer.SaveCurrentFrame();

        foreach (var (player, input) in addedPlayers)
        {
            if (inRollback && savedFrames.Count > 0)
                input.Last = savedFrames.Peek().GetInput(player);
            else
                input.Last = input.Current;

            inputBuffer[player.Index] = input.Last.Data;
            syncInputBuffer[player.Index] = new(input.Last.Data, false);
        }

        random.UpdateSeed(CurrentFrame, inputBuffer, extraSeedState);
        return ResultCode.Ok;
    }

    uint extraSeedState;

    public void SetRandomSeed(uint seed, uint extraState = 0) => extraSeedState = unchecked(seed + extraState);

    public bool LoadFrame(Frame frame)
    {
        frame = Frame.Max(in frame, in Frame.Zero);
        return synchronizer.TryLoadFrame(in frame);
    }

    public void AdvanceFrame()
    {
        logger.Write(LogLevel.Trace, $"End of frame ({synchronizer.CurrentFrame.Number})...");

        synchronizer.IncrementFrame();

        GameInput<ConfirmedInputs<TInput>> lastInputs = new(synchronizer.CurrentFrame);
        lastInputs.Data.Count = (byte)addedPlayers.Count;

        foreach (var (player, input) in addedPlayers)
        {
            input.Current.Erase();
            lastInputs.Data.Inputs[player.Index] = input.Last.Data;
        }

        if (inRollback) return;

        // Hold onto the current frame in our queue of saved states.
        // We'll need the checksum later to verify that our replay of the same frame got the same results.
        var lastSaved = synchronizer.GetLastSavedFrame();
        var stateBytes = ArrayPool<byte>.Shared.Rent(lastSaved.GameState.WrittenCount);
        lastSaved.GameState.WrittenSpan.CopyTo(stateBytes);

        var frame = synchronizer.CurrentFrame;
        savedFrames.Enqueue(new(
            Frame: frame,
            Inputs: lastInputs,
            State: stateBytes,
            StateSize: lastSaved.GameState.WrittenCount,
            Checksum: lastSaved.Checksum
        ));

        if (frame - lastVerified < checkDistance.FrameValue)
            return;

        // We've gone far enough ahead and should now start replaying frames.
        // Load the last verified frame and set the rollback flag to true.
        synchronizer.LoadFrame(in lastVerified);

        inRollback = true;
        while (savedFrames.Count > 0)
        {
            callbacks.AdvanceFrame();

            // Verify that the checksum of this frame is the same as the one in our list.
            var current = savedFrames.Dequeue();

            try
            {
                if (current.Frame != synchronizer.CurrentFrame)
                {
                    var message = $"Frame number {current.Frame.Number} does not match saved frame number {frame}";
                    logger.Write(LogLevel.Error, message);
                    if (throwError)
                        throw new NetcodeException(message);
                }

                var last = synchronizer.GetLastSavedFrame();
                if (current.Checksum != last.Checksum)
                    HandleDesync(frame, current, last);
                else
                    logger.Write(LogLevel.Trace,
                        $"Checksum #{last.Checksum:x8} for frame {current.Frame.Number} matches");
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(current.State);
            }
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


        var currentOffset = 0;
        var currentBytes = current.State.AsSpan(0, current.StateSize);
        BinaryBufferReader currentReader = new(currentBytes, ref currentOffset, endianness);
        var currentObject = callbacks.CreateState(current.Frame, in currentReader);
        var currentBody = stateParser.GetStateString(current.Frame, in currentReader, currentObject);
        LogSaveState(level, "CURRENT", currentBody, current.Checksum, current.Frame, current.Inputs.Frame.Number);

        var lastOffset = 0;
        BinaryBufferReader previousReader = new(previous.GameState.WrittenSpan, ref lastOffset, endianness);
        var previousObject = callbacks.CreateState(current.Frame, in previousReader);
        var previousBody = stateParser.GetStateString(current.Frame, in previousReader, previousObject);
        LogSaveState(level, "LAST", previousBody, previous.Checksum, previous.Frame);

        if (mismatchHandler is not null)
        {
            (currentOffset, lastOffset) = (0, 0);
            mismatchHandler.Handle(
                this,
                new(previousBody, in previousReader, previous.Checksum, previousObject),
                new(currentBody, in currentReader, current.Checksum, currentObject)
            );
        }

        if (throwError) throw new NetcodeException(message);
    }

    void LogSaveState(LogLevel level,
        string description, string body,
        uint checksum, Frame frame,
        object? extra = null
    )
    {
        if (!logStateOnDesync) return;
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

    public void SetFrameDelay(NetcodePlayer player, int delayInFrames)
    {
        ThrowIf.ArgumentOutOfBounds(player.Index, 0, addedPlayers.Count);
        ArgumentOutOfRangeException.ThrowIfNegative(delayInFrames);
        synchronizer.SetFrameDelay(player, delayInFrames);
    }

    [MemberNotNull(nameof(callbacks))]
    public void SetHandler(INetcodeSessionHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);
        callbacks = handler;
        synchronizer.Callbacks = handler;
    }
}
