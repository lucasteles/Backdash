using System.Text;
using System.Text.Json;
using Backdash.Core;
using Backdash.Data;
using Backdash.Network;
using Backdash.Sync;
using Backdash.Sync.State;

namespace Backdash.Backends;

sealed class SyncTestBackend<TInput, TGameState> : IRollbackSession<TInput, TGameState>
    where TInput : struct
    where TGameState : IEquatable<TGameState>, new()
{
    readonly record struct SavedFrame(Frame Frame, int Checksum, TGameState State, GameInput<TInput> Input);

    readonly TaskCompletionSource tsc = new();
    readonly Logger logger;
    readonly HashSet<PlayerHandle> addedPlayers = [];
    readonly HashSet<PlayerHandle> addedSpectators = new();
    readonly Synchronizer<TInput, TGameState> synchronizer;

    readonly Queue<SavedFrame> savedFrames = [];

    readonly JsonSerializerOptions jsonOptions = new()
    {
        WriteIndented = true,
    };

    IRollbackHandler<TGameState> callbacks;

    bool rollingback;
    GameInput<TInput> currentInput;
    GameInput<TInput> lastInput;

    Frame checkDistance = Frame.Zero;
    Frame lastVerified = Frame.Zero;

    bool running;
    Task backGroundJobTask = Task.CompletedTask;

    public SyncTestBackend(
        RollbackOptions options,
        IStateStore<TGameState> stateStore,
        IChecksumProvider<TGameState> checksumProvider,
        Logger logger
    )
    {
        ThrowHelpers.ThrowIfTypeTooBigForStack<TInput>();
        ThrowHelpers.ThrowIfTypeTooBigForStack<GameInput<TInput>>();
        ThrowHelpers.ThrowIfArgumentIsZeroOrLess(options.LocalPort);

        this.logger = logger;
        callbacks ??= new EmptySessionHandler<TGameState>(logger);

        synchronizer = new(
            options, logger,
            addedPlayers,
            stateStore,
            checksumProvider,
            new(Max.RemoteConnections)
        )
        {
            Callbacks = callbacks,
        };
        currentInput = new();
        lastInput = new();
    }

    public void Dispose() => tsc.SetResult();

    public int NumberOfPlayers => addedPlayers.Count;
    public int NumberOfSpectators => addedSpectators.Count;
    public IReadOnlyCollection<PlayerHandle> GetPlayers() => addedPlayers;
    public IReadOnlyCollection<PlayerHandle> GetSpectators() => addedSpectators;

    public void Start(CancellationToken stoppingToken = default)
    {
        if (!running)
        {
            callbacks.Start();
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
        if (addedPlayers.Count >= Max.Players)
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

    public void AddPlayers(IEnumerable<Player> players)
    {
        foreach (var player in players)
            AddPlayer(player);
    }

    public bool IsConnected(in PlayerHandle player) => addedPlayers.Contains(player);

    public bool GetInfo(in PlayerHandle player, ref RollbackSessionInfo info) => false;

    public ResultCode AddLocalInput(PlayerHandle player, TInput localInput)
    {
        if (!running)
            return ResultCode.NotSynchronized;

        currentInput.Data = localInput;
        return ResultCode.Ok;
    }

    public void BeginFrame() => logger.Write(LogLevel.Trace, $"Beginning of frame({synchronizer.FrameCount})...");


    public ResultCode SynchronizeInputs(Span<TInput> inputs)
    {
        if (rollingback)
        {
            lastInput = savedFrames.Peek().Input;
        }
        else
        {
            if (synchronizer.FrameCount == Frame.Zero)
                synchronizer.SaveCurrentFrame();

            lastInput = currentInput;
        }

        inputs[0] = lastInput.Data;

        return ResultCode.Ok;
    }

    public void AdvanceFrame()
    {
        logger.Write(LogLevel.Trace, $"End of frame({synchronizer.FrameCount})...");
        synchronizer.IncrementFrame();
        currentInput.Erase();
        if (rollingback) return;

        // Hold onto the current frame in our queue of saved states.  We'll need
        // the checksum later to verify that our replay of the same frame got the
        // same results.
        var lastSaved = synchronizer.GetLastSavedFrame();

        var frame = synchronizer.FrameCount;
        savedFrames.Enqueue(new(
            Frame: frame,
            Input: lastInput,
            State: lastSaved.GameState,
            Checksum: lastSaved.Checksum
        ));

        if (frame - lastVerified == checkDistance)
        {
            // We've gone far enough ahead and should now start replaying frames.
            // Load the last verified frame and set the rollback flag to true.
            synchronizer.LoadFrame(in lastVerified);

            rollingback = true;
            while (savedFrames.Count > 0)
            {
                callbacks.AdvanceFrame();

                // Verify that the checksumn of this frame is the same as the one in our
                // list.
                var info = savedFrames.Dequeue();

                if (info.Frame != synchronizer.FrameCount)
                {
                    logger.Write(LogLevel.Error,
                        $"Frame number {info.Frame} does not match saved frame number {frame}");
                }

                var last = synchronizer.GetLastSavedFrame();
                var checksum = last.Checksum;
                if (info.Checksum != checksum)
                {
                    LogSaveState(info);
                    logger.Write(LogLevel.Error,
                        $"Checksum for frame {frame} does not match saved ({checksum} != {info.Checksum})");
                }

                logger.Write(LogLevel.Trace, $"Checksum {checksum} for frame {info.Frame} matches");
            }

            lastVerified = frame;
            rollingback = false;
        }
    }

    void LogSaveState(SavedFrame info)
    {
        if (logger.EnabledLevel > LogLevel.Debug)
            return;

        StringBuilder builder = new();
        builder.AppendLine($"=== Saved State ({info.Frame}) ");
        var input = info.Input;
        builder.AppendLine("--- Input");
        builder.AppendLine(input.Data.ToString());
        builder.AppendLine();
        builder.AppendLine($"--- Game State #{info.Checksum}");
        builder.AppendLine(JsonSerializer.Serialize(info.State, jsonOptions));
        builder.AppendLine("======================");

        logger.Write(LogLevel.Debug, $"{builder.ToString()}");
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
