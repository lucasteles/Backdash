using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Backdash.Core;
using Backdash.Data;
using Backdash.Network;
using Backdash.Serialization;
using Backdash.Sync;

namespace Backdash.Backends;

sealed class SyncTestBackend<TInput, TGameState> : IRollbackSession<TInput, TGameState>
    where TInput : struct where TGameState : notnull
{
    readonly record struct SavedFrame(Frame Frame, int Checksum, TGameState State, GameInput Input);

    readonly TaskCompletionSource tsc = new();
    readonly Logger logger;
    readonly RollbackOptions options;
    readonly IBinarySerializer<TInput> inputSerializer;
    readonly Queue<SavedFrame> savedFrames = [];
    readonly HashSet<PlayerHandle> players = [];
    readonly Synchronizer<TInput, TGameState> synchronizer;

    readonly JsonSerializerOptions jsonOptions = new()
    {
        WriteIndented = true,
    };

    IRollbackHandler<TGameState> callbacks;

    bool rollingback;
    GameInput currentInput;
    GameInput lastInput;

    Frame checkDistance = Frame.Zero;
    Frame lastVerified = Frame.Zero;

    bool running;
    Task backGroundJobTask = Task.CompletedTask;

    public SyncTestBackend(
        IRollbackHandler<TGameState> callbacks,
        RollbackOptions options,
        IBinarySerializer<TInput> inputSerializer,
        Logger logger
    )
    {
        ThrowHelpers.ThrowIfArgumentIsZeroOrLess(options.LocalPort);
        ThrowHelpers.ThrowIfArgumentIsZeroOrLess(options.NumberOfPlayers);
        ThrowHelpers.ThrowIfTypeTooBigForStack<GameInput>();
        ThrowHelpers.ThrowIfTypeSizeGreaterThan<GameInputBuffer>(Max.InputSizeInBytes);
        ThrowHelpers.ThrowIfTypeTooBigForStack<TInput>();


        var inputTypeSize = inputSerializer.GetTypeSize();
        ThrowHelpers.ThrowIfArgumentOutOfBounds(inputTypeSize, 1, Max.InputSizeInBytes);
        Trace.Assert(options.SpectatorOffset > options.NumberOfPlayers);

        this.options = options;
        this.inputSerializer = inputSerializer;
        this.logger = logger;
        this.options.InputSize = inputTypeSize;

        this.callbacks = callbacks;
        synchronizer = new(options, logger, inputSerializer, new(Max.RemoteConnections))
        {
            Callbacks = this.callbacks,
        };
        currentInput = GameInput.Create(this.options.InputSize);
        lastInput = GameInput.Create(this.options.InputSize);
    }

    public void Dispose() => tsc.SetResult();

    public void Start(CancellationToken stoppingToken = default)
    {
        if (!running)
        {
            callbacks.OnEvent(new()
            {
                Type = RollbackEventType.Running,
            });
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
        if (player.Number < 1 || player.Number > options.NumberOfPlayers)
            return ResultCode.PlayerOutOfRange;

        players.Add(player.Handle);

        return ResultCode.Ok;
    }

    public bool IsConnected(in PlayerHandle player) => players.Contains(player);

    public bool GetInfo(in PlayerHandle player, ref RollbackSessionInfo info) => false;

    public ResultCode AddLocalInput(PlayerHandle player, TInput localInput)
    {
        if (!running)
            return ResultCode.NotSynchronized;

        inputSerializer.Serialize(ref localInput, currentInput.Buffer);

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

        inputs[0] = inputSerializer.Deserialize(lastInput.Buffer);

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
        ref var lastSaved = ref synchronizer.GetLastSavedFrame();

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

                int checksum = synchronizer.GetLastSavedFrame().Checksum;
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
        builder.AppendLine($"--- Input: ({(ByteSize)input.Size})");
        builder.AppendLine(inputSerializer.Deserialize(input.Buffer).ToString());
        builder.AppendLine();
        builder.AppendLine($"--- Game State #{info.Checksum}");
        builder.AppendLine(JsonSerializer.Serialize(info.State, jsonOptions));
        builder.AppendLine("======================");

        logger.Write(LogLevel.Debug, $"{builder.ToString()}");
    }

    public void SetFrameDelay(PlayerHandle player, int delayInFrames)
    {
        ThrowHelpers.ThrowIfArgumentOutOfBounds(player.Number, 1, options.NumberOfPlayers);
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
