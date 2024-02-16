using System.Diagnostics;
using Backdash.Core;
using Backdash.Data;
using Backdash.Network;
using Backdash.Serialization;

namespace Backdash.Sync;

sealed class Synchronizer<TInput, TState>
    where TInput : struct
    where TState : notnull
{
    readonly RollbackOptions options;
    readonly Logger logger;
    readonly IBinarySerializer<TInput> inputSerializer;
    readonly ConnectionsState localConnections;
    readonly InputQueue[] inputQueues;
    readonly SavedFrame[] savedStates;

    public required IRollbackHandler<TState> Callbacks { get; internal set; }

    int head;
    bool rollingBack;
    Frame frameCount = Frame.Zero;
    Frame lastConfirmedFrame = Frame.Zero;

    public Synchronizer(
        RollbackOptions options,
        Logger logger,
        IBinarySerializer<TInput> inputSerializer,
        ConnectionsState localConnections
    )
    {
        Trace.Assert(options.InputSize > 0);

        this.options = options;
        this.logger = logger;
        this.inputSerializer = inputSerializer;
        this.localConnections = localConnections;

        savedStates = new SavedFrame[options.PredictionFrames + options.PredictionFramesOffset];
        Array.Fill(savedStates, new SavedFrame(Frame.Null, default!, 0));

        inputQueues = new InputQueue[options.NumberOfPlayers];
        for (var i = 0; i < inputQueues.Length; i++)
            inputQueues[i] = new(options.InputSize, options.Protocol.MaxInputQueue, logger)
            {
                FrameDelay = Math.Max(options.FrameDelay, 0)
            };
    }

    public bool InRollback => rollingBack;
    public Frame FrameCount => frameCount;
    public Frame FramesBehind => Frame.Max(frameCount - lastConfirmedFrame, Frame.Zero);

    public void SetLastConfirmedFrame(in Frame frame)
    {
        lastConfirmedFrame = frame;

        if (lastConfirmedFrame <= Frame.Zero)
            return;

        for (var i = 0; i < inputQueues.Length; i++)
            inputQueues[i].DiscardConfirmedFrames(frame.Previous());
    }

    void AddInput(in PlayerHandle queue, ref GameInput input) => inputQueues[queue.Index].AddInput(ref input);

    public bool AddLocalInput(in PlayerHandle queue, ref GameInput input)
    {
        if (frameCount >= options.PredictionFrames && FramesBehind >= options.PredictionFrames)
        {
            logger.Write(LogLevel.Warning, "Rejecting input from emulator: reached prediction barrier.");
            return false;
        }

        if (frameCount == 0)
            SaveCurrentFrame();

        logger.Write(LogLevel.Debug, $"Sending non-delayed local frame {frameCount} to queue {queue}.");
        input.Frame = frameCount;
        AddInput(in queue, ref input);

        return true;
    }

    public void AddRemoteInput(in PlayerHandle player, GameInput input) => AddInput(in player, ref input);

    public bool GetConfirmedInput(in Frame frame, int playerNumber, out GameInput confirmed)
    {
        confirmed = GameInput.Create(options.InputSize, frame);
        if (localConnections[playerNumber].Disconnected && frame > localConnections[playerNumber].LastFrame)
            return false;

        inputQueues[playerNumber].GetConfirmedInput(in frame, ref confirmed);
        return true;
    }

    public void SynchronizeInputs(Span<TInput> output, out bool disconnections)
    {
        disconnections = false;
        Trace.Assert(output.Length >= options.NumberOfPlayers);
        output.Clear();

        for (var i = 0; i < options.NumberOfPlayers; i++)
        {
            var input = GameInput.Create(options.InputSize);
            if (localConnections[i].Disconnected && frameCount > localConnections[i].LastFrame)
                disconnections = true;
            else
                inputQueues[i].GetInput(frameCount, out input);

            ReadOnlySpan<byte> inputBytes = input.Buffer;
            output[i] = inputSerializer.Deserialize(in inputBytes);
        }
    }

    public void CheckSimulation()
    {
        if (!CheckSimulationConsistency(out var seekTo))
            AdjustSimulation(in seekTo);
    }

    public void IncrementFrame()
    {
        frameCount++;
        SaveCurrentFrame();
    }

    public void AdjustSimulation(in Frame seekTo)
    {
        var currentCount = frameCount;
        var count = frameCount - seekTo;

        logger.Write(LogLevel.Debug, "Catching up");
        rollingBack = true;

        /*
         * Flush our input queue and load the last frame.
         */
        LoadFrame(in seekTo);
        Trace.Assert(frameCount == seekTo);

        /*
         * Advance frame by frame (stuffing notifications back to
         * the master).
         */
        ResetPrediction(in frameCount);
        for (var i = 0; i < count.Number; i++)
            Callbacks.AdvanceFrame();

        Trace.Assert(frameCount == currentCount);
        rollingBack = false;
    }

    public void LoadFrame(in Frame frame)
    {
        // find the frame in question
        if (frame == frameCount)
        {
            logger.Write(LogLevel.Trace, "Skipping NOP.");
            return;
        }

        ref var savedFrame = ref FindSavedFrame(in frame, setHead: true);

        logger.Write(LogLevel.Debug,
            $"* Loading frame info {savedFrame.Frame} (checksum: {savedFrame.Checksum})");

        var state = savedFrame.GameState;
        Callbacks.LoadGameState(in state);

        // Reset framecount and the head of the state ring-buffer to point in
        // advance of the current frame (as if we had just finished executing it).
        frameCount = savedFrame.Frame;
    }

    public ref SavedFrame GetLastSavedFrame() => ref savedStates[^1];

    void AdvanceHead() => head = (head + 1) % savedStates.Length;

    public void SaveCurrentFrame()
    {
        ref var next = ref savedStates[head];
        next.Checksum = 0;
        next.Frame = frameCount;
        Callbacks.SaveGameState(frameCount.Number, ref next.Checksum, out next.GameState);
        if (next.Checksum is 0)
            next.Checksum = EqualityComparer<TState>.Default.GetHashCode(next.GameState);

        logger.Write(LogLevel.Debug, $"* Saved frame {next.Frame} (checksum: {next.Checksum}).");
        AdvanceHead();
    }

    ref SavedFrame FindSavedFrame(in Frame frame, bool setHead = false)
    {
        for (var i = 0; i < savedStates.Length; i++)
        {
            ref var current = ref savedStates[i];
            if (current.Frame == frame)
            {
                if (setHead)
                {
                    head = i;
                    AdvanceHead();
                }

                return ref current;
            }
        }

        throw new BackdashException($"Invalid state frame search: {frame}");
    }

    bool CheckSimulationConsistency(out Frame seekTo)
    {
        var firstIncorrect = Frame.Null;
        for (var i = 0; i < options.NumberOfPlayers; i++)
        {
            var incorrect = inputQueues[i].GetFirstIncorrectFrame();
            if (incorrect.IsNull || (firstIncorrect.IsNotNull && incorrect >= firstIncorrect))
                continue;
            logger.Write(LogLevel.Debug, $"Incorrect frame {incorrect} reported by queue {i}");
            firstIncorrect = incorrect;
        }

        if (firstIncorrect.IsNull)
        {
            logger.Write(LogLevel.Debug, "Prediction ok.  proceeding.");
            seekTo = default;
            return true;
        }

        seekTo = firstIncorrect;
        return false;
    }

    public void SetFrameDelay(PlayerHandle player, int delay) => inputQueues[player.Index].FrameDelay = delay;

    void ResetPrediction(in Frame frameNumber)
    {
        for (var i = 0; i < inputQueues.Length; i++)
            inputQueues[i].ResetPrediction(in frameNumber);
    }

    public struct SavedFrame(Frame frame, TState state, int checksum)
    {
        public Frame Frame = frame;
        public TState GameState = state;
        public int Checksum = checksum;
    }
}
