using Backdash.Backends;
using Backdash.Core;
using Backdash.Data;
using Backdash.Network;
using Backdash.Serialization;

namespace Backdash.Input;

sealed class Synchronizer<TInput, TState>
    where TInput : struct
    where TState : notnull
{
    readonly RollbackOptions options;
    readonly ILogger logger;
    readonly IBinarySerializer<TInput> inputSerializer;
    readonly ConnectionStatuses localConnections;
    readonly InputQueue[] inputQueues;
    readonly IRollbackHandler<TState> callbacks;
    readonly CircularBuffer<SavedFrame> savedStates;

    bool rollingback;
    Frame frameCount = Frame.Zero;
    Frame lastConfirmedFrame = Frame.Zero;

    public Synchronizer(
        RollbackOptions options,
        IRollbackHandler<TState> callbacks,
        ILogger logger,
        IBinarySerializer<TInput> inputSerializer,
        ConnectionStatuses localConnections
    )
    {
        Tracer.Assert(options.InputSize > 0);

        this.options = options;
        this.callbacks = callbacks;
        this.logger = logger;
        this.inputSerializer = inputSerializer;
        this.localConnections = localConnections;

        savedStates = new(options.PredictionFrames + options.PredictionFramesOffset);
        inputQueues = new InputQueue[options.NumberOfPlayers];
        for (var i = 0; i < inputQueues.Length; i++)
            inputQueues[i] = new(options.InputSize, options.InputQueueLength, logger);
    }

    public bool InRollback() => rollingback;

    void SetLastConfirmedFrame(in Frame frame)
    {
        lastConfirmedFrame = frame;

        if (lastConfirmedFrame > Frame.Zero)
            for (var i = 0; i < inputQueues.Length; i++)
                inputQueues[i].DiscardConfirmedFrames(frame.Previous);
    }

    void AddInput(in QueueIndex queue, ref GameInput input) => inputQueues[queue.Number].AddInput(ref input);

    public bool AddLocalInput(QueueIndex queue, GameInput input)
    {
        var framesBehind = frameCount - lastConfirmedFrame;
        if (frameCount >= options.PredictionFrames && framesBehind >= options.PredictionFrames)
        {
            logger.Info($"Rejecting input from emulator: reached prediction barrier.");
            return false;
        }

        if (frameCount == 0)
            SaveCurrentFrame();

        logger.Info($"Sending non-delayed local frame {frameCount} to queue {queue}.");
        input.Frame = frameCount;
        AddInput(in queue, ref input);

        return true;
    }

    public void AddRemoteInput(QueueIndex queue, GameInput input) => AddInput(in queue, ref input);

    public DisconnectFlags GetConfirmedInputs(in Frame frame, Span<TInput> output)
    {
        Tracer.Assert(output.Length >= options.NumberOfPlayers * options.InputSize);

        var disconnectFlags = 0;
        output.Clear();

        for (var i = 0; i < options.NumberOfPlayers; i++)
        {
            var input = GameInput.OfSize(options.InputSize);
            if (localConnections[i].Disconnected && frame > localConnections[i].LastFrame)
                disconnectFlags |= 1 << i;
            else
                inputQueues[i].GetConfirmedInput(in frame, ref input);

            ReadOnlySpan<byte> inputBytes = input.Buffer;
            output[i] = inputSerializer.Deserialize(in inputBytes);
        }

        return new(disconnectFlags);
    }

    public DisconnectFlags SynchronizeInputs(Span<TInput> output)
    {
        var disconnectFlags = 0;
        Tracer.Assert(output.Length >= options.NumberOfPlayers * options.InputSize);
        output.Clear();

        for (int i = 0; i < options.NumberOfPlayers; i++)
        {
            var input = GameInput.OfSize(options.InputSize);
            if (localConnections[i].Disconnected && frameCount > localConnections[i].LastFrame)
                disconnectFlags |= 1 << i;
            else
                inputQueues[i].GetInput(frameCount, out input);

            ReadOnlySpan<byte> inputBytes = input.Buffer;
            output[i] = inputSerializer.Deserialize(in inputBytes);
        }

        return new(disconnectFlags);
    }

    void CheckSimulation()
    {
        if (!CheckSimulationConsistency(out var seekTo))
            AdjustSimulation(in seekTo);
    }

    void IncrementFrame()
    {
        frameCount++;
        SaveCurrentFrame();
    }

    void AdjustSimulation(in Frame seekTo)
    {
        var currentCount = frameCount;
        var count = frameCount - seekTo;

        logger.Info($"Catching up");
        rollingback = true;

        /*
         * Flush our input queue and load the last frame.
         */
        LoadFrame(in seekTo);
        Tracer.Assert(frameCount == seekTo);

        /*
         * Advance frame by frame (stuffing notifications back to
         * the master).
         */
        ResetPrediction(in frameCount);
        for (var i = 0; i < count.Number; i++)
            callbacks.AdvanceFrame();

        Tracer.Assert(frameCount == currentCount);
        rollingback = false;
    }

    void LoadFrame(in Frame frame)
    {
        // find the frame in question
        if (frame == frameCount)
        {
            logger.Info($"Skipping NOP.");
            return;
        }

        ref var savedFrame = ref FindSavedFrame(in frame, setHead: true);

        logger.Info($"=== Loading frame info {savedFrame.Frame} (checksum: {savedFrame.Checksum})");

        var state = savedFrame.GameState;
        callbacks.LoadGameState(in state);

        // Reset framecount and the head of the state ring-buffer to point in
        // advance of the current frame (as if we had just finished executing it).
        frameCount = savedFrame.Frame;
    }

    void SaveCurrentFrame()
    {
        /*
         * See StateCompress for the real save feature implemented by FinalBurn.
         * Write everything into the head, then advance the head pointer.
         */
        var checksum = 0;
        callbacks.SaveGameState(frameCount.Number, ref checksum, out var gameState);
        if (checksum is 0)
            checksum = EqualityComparer<TState>.Default.GetHashCode(gameState);

        SavedFrame state = new(frameCount, gameState, checksum);

        logger.Info($"=== Saved frame info {state.Frame} (checksum: {state.Checksum}).");
        savedStates.Add(state);
    }

    ref SavedFrame GetLastSavedFrame() => ref savedStates.Peek();

    ref SavedFrame FindSavedFrame(in Frame frame, bool setHead = false)
    {
        for (var i = 0; i < savedStates.Length; i++)
        {
            ref SavedFrame current = ref savedStates[i];
            if (current.Frame == frame)
            {
                if (setHead)
                {
                    savedStates.SetHeadTo(i);
                    savedStates.Advance();
                }

                return ref current;
            }
        }

        throw new BackdashException($"Invalid state frame search: {frame}");
    }

    bool CheckSimulationConsistency(out Frame seekTo)
    {
        var firstIncorrect = Frame.Null;
        for (int i = 0; i < options.NumberOfPlayers; i++)
        {
            var incorrect = inputQueues[i].GetFirstIncorrectFrame();
            if (incorrect.IsNull || (firstIncorrect.IsValid && incorrect >= firstIncorrect))
                continue;
            logger.Info($"Incorrect frame {incorrect} reported by queue {i}");
            firstIncorrect = incorrect;
        }

        if (firstIncorrect.IsNull)
        {
            logger.Info($"Prediction ok.  proceeding.");
            seekTo = default;
            return true;
        }

        seekTo = firstIncorrect;
        return false;
    }

    public void SetFrameDelay(QueueIndex queue, int delay) => inputQueues[queue.Number].FrameDelay = delay;

    void ResetPrediction(in Frame frameNumber)
    {
        for (var i = 0; i < inputQueues.Length; i++)
            inputQueues[i].ResetPrediction(in frameNumber);
    }

    public readonly record struct SavedFrame(Frame Frame, TState GameState, int Checksum);
}
