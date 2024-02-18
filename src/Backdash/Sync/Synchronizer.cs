using System.Diagnostics;
using Backdash.Core;
using Backdash.Data;
using Backdash.Network;
using Backdash.Sync.State;

namespace Backdash.Sync;

sealed class Synchronizer<TInput, TState>
    where TInput : struct
    where TState : IEquatable<TState>
{
    readonly RollbackOptions options;
    readonly Logger logger;
    readonly IStateStore<TState> stateStore;
    readonly IChecksumProvider<TState> checksumProvider;
    readonly ConnectionsState localConnections;
    readonly InputQueue<TInput>[] inputQueues;
    public required IRollbackHandler<TState> Callbacks { get; internal set; }

    Frame frameCount = Frame.Zero;
    Frame lastConfirmedFrame = Frame.Zero;

    public Synchronizer(
        RollbackOptions options,
        Logger logger,
        IStateStore<TState> stateStore,
        IChecksumProvider<TState> checksumProvider,
        ConnectionsState localConnections
    )
    {
        this.options = options;
        this.logger = logger;
        this.stateStore = stateStore;
        this.checksumProvider = checksumProvider;
        this.localConnections = localConnections;

        stateStore.Initialize(options.PredictionFrames + options.PredictionFramesOffset);

        inputQueues = new InputQueue<TInput>[options.NumberOfPlayers];
        var delay = Math.Max(options.FrameDelay, 0);
        for (var i = 0; i < inputQueues.Length; i++)
            inputQueues[i] = new(options.Protocol.MaxInputQueue, logger)
            {
                FrameDelay = delay,
            };
    }

    public bool InRollback { get; private set; }
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

    void AddInput(in PlayerHandle queue, ref GameInput<TInput> input) => inputQueues[queue.Index].AddInput(ref input);

    public bool AddLocalInput(in PlayerHandle queue, ref GameInput<TInput> input)
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

    public void AddRemoteInput(in PlayerHandle player, GameInput<TInput> input) => AddInput(in player, ref input);

    public bool GetConfirmedInput(in Frame frame, int playerNumber, out GameInput<TInput> confirmed)
    {
        confirmed = new(frame);
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
            if (localConnections[i].Disconnected && frameCount > localConnections[i].LastFrame)
            {
                disconnections = true;
                output[i] = default;
            }
            else
            {
                inputQueues[i].GetInput(frameCount, out var input);
                output[i] = input.Data;
            }
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
        InRollback = true;

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
        InRollback = false;
    }

    public void LoadFrame(in Frame frame)
    {
        // find the frame in question
        if (frame == frameCount)
        {
            logger.Write(LogLevel.Trace, "Skipping NOP.");
            return;
        }

        var savedFrame = stateStore.Load(frame);

        logger.Write(LogLevel.Debug,
            $"* Loading frame info {savedFrame.Frame} (checksum: {savedFrame.Checksum})");

        var state = savedFrame.GameState;
        Callbacks.LoadGameState(in state);

        // Reset framecount and the head of the state ring-buffer to point in
        // advance of the current frame (as if we had just finished executing it).
        frameCount = savedFrame.Frame;
    }

    public SavedFrame<TState> GetLastSavedFrame() => stateStore.Last();

    public void SaveCurrentFrame()
    {
        Callbacks.SaveGameState(frameCount.Number, out var newState);

        SavedFrame<TState> next = new()
        {
            Frame = frameCount,
            GameState = newState,
            Checksum = checksumProvider.Compute(in newState),
        };
        stateStore.Save(in next);

        logger.Write(LogLevel.Debug, $"* Saved frame {next.Frame} (checksum: {next.Checksum}).");
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
}
