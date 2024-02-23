using System.Diagnostics;
using Backdash.Core;
using Backdash.Data;
using Backdash.Network;
using Backdash.Sync.State;

namespace Backdash.Sync;

sealed class Synchronizer<TInput, TState>
    where TInput : struct
    where TState : IEquatable<TState>, new()
{
    readonly RollbackOptions options;
    readonly Logger logger;
    readonly IReadOnlyCollection<PlayerHandle> players;
    readonly IStateStore<TState> stateStore;
    readonly IChecksumProvider<TState> checksumProvider;
    readonly ConnectionsState localConnections;
    readonly List<InputQueue<TInput>> inputQueues;

    public required IRollbackHandler<TState> Callbacks { get; internal set; }

    Frame currentFrame = Frame.Zero;
    Frame lastConfirmedFrame = Frame.Zero;
    int NumberOfPlayers => players.Count;

    public Synchronizer(
        RollbackOptions options,
        Logger logger,
        IReadOnlyCollection<PlayerHandle> players,
        IStateStore<TState> stateStore,
        IChecksumProvider<TState> checksumProvider,
        ConnectionsState localConnections
    )
    {
        this.options = options;
        this.logger = logger;
        this.players = players;
        this.stateStore = stateStore;
        this.checksumProvider = checksumProvider;
        this.localConnections = localConnections;

        stateStore.Initialize(options.PredictionFrames + options.PredictionFramesOffset);
        inputQueues = new(2);
    }

    public bool InRollback { get; private set; }
    public Frame CurrentFrame => currentFrame;
    public FrameSpan FramesBehind => new(currentFrame.Number - lastConfirmedFrame.Number, options.FramesPerSecond);

    public FrameSpan RollbackFrames { get; private set; } = FrameSpan.Zero;

    public void AddQueue(PlayerHandle player)
    {
        var delay = Math.Max(options.FrameDelay, 0);
        inputQueues.Add(new(player.InternalQueue, options.InputQueueLength, logger)
        {
            FrameDelay = delay,
        });
    }

    public void SetLastConfirmedFrame(in Frame frame)
    {
        lastConfirmedFrame = frame;

        if (lastConfirmedFrame <= Frame.Zero)
            return;

        var discardUntil = frame.Previous();

        for (var i = 0; i < NumberOfPlayers; i++)
            inputQueues[i].DiscardConfirmedFrames(discardUntil);
    }

    void AddInput(in PlayerHandle queue, ref GameInput<TInput> input) =>
        inputQueues[queue.InternalQueue].AddInput(ref input);

    public bool AddLocalInput(in PlayerHandle queue, ref GameInput<TInput> input)
    {
        if (currentFrame >= options.PredictionFrames && FramesBehind.FrameCount >= options.PredictionFrames)
        {
            logger.Write(LogLevel.Warning,
                $"Rejecting input for frame {currentFrame.Number} from emulator: reached prediction barrier");
            return false;
        }

        if (currentFrame == 0)
            SaveCurrentFrame();

        logger.Write(LogLevel.Debug, $"Sending non-delayed local frame {currentFrame} to queue {queue}");
        input.Frame = currentFrame;
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

    public void SynchronizeInputs(Span<SynchronizedInput<TInput>> output)
    {
        Trace.Assert(output.Length >= NumberOfPlayers);
        output.Clear();

        for (var i = 0; i < NumberOfPlayers; i++)
        {
            if (localConnections[i].Disconnected && currentFrame > localConnections[i].LastFrame)
            {
                output[i] = new(default, true);
            }
            else
            {
                inputQueues[i].GetInput(currentFrame, out var input);
                output[i] = new(input.Data, false);
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
        currentFrame++;
        SaveCurrentFrame();
    }

    public void AdjustSimulation(in Frame seekTo)
    {
        var localCurrentFrame = currentFrame;
        var rollbackCount = currentFrame.Number - seekTo.Number;

        logger.Write(LogLevel.Debug, $"Catching up. rolling back {rollbackCount} frames");
        InRollback = true;

        // Flush our input queue and load the last frame.
        LoadFrame(in seekTo);
        Trace.Assert(currentFrame == seekTo);

        // Advance frame by frame (stuffing notifications back to the master).
        ResetPrediction(in currentFrame);
        for (var i = 0; i < rollbackCount; i++)
        {
            logger.Write(LogLevel.Debug, $"[Begin Frame {currentFrame}](rollback)");
            Callbacks.AdvanceFrame();
        }

        Trace.Assert(currentFrame == localCurrentFrame);
        InRollback = false;
    }

    public void LoadFrame(in Frame frame)
    {
        // find the frame in question
        if (frame == currentFrame)
        {
            logger.Write(LogLevel.Trace, "Skipping NOP.");
            return;
        }

        ref readonly var savedFrame = ref stateStore.Load(frame);

        logger.Write(LogLevel.Debug,
            $"* Loading frame info {savedFrame.Frame} (checksum: {savedFrame.Checksum})");

        Callbacks.LoadState(in savedFrame.GameState);

        // Reset framecount and the head of the state ring-buffer to point in
        // advance of the current frame (as if we had just finished executing it).
        currentFrame = savedFrame.Frame;
    }

    public ref readonly SavedFrame<TState> GetLastSavedFrame() => ref stateStore.Last();

    public void SaveCurrentFrame()
    {
        ref var nextState = ref stateStore.GetCurrent();
        Callbacks.ClearState(ref nextState);
        Callbacks.SaveState(currentFrame.Number, ref nextState);

        var checksum = checksumProvider.Compute(in nextState);
        ref readonly var next = ref stateStore.SaveCurrent(in currentFrame, in checksum);
        logger.Write(LogLevel.Debug, $"* Saved frame {next.Frame} (checksum: {next.Checksum}).");
    }

    bool CheckSimulationConsistency(out Frame seekTo)
    {
        var firstIncorrect = Frame.Null;
        for (var i = 0; i < NumberOfPlayers; i++)
        {
            var incorrect = inputQueues[i].FirstIncorrectFrame;
            if (incorrect.IsNull || (firstIncorrect.IsNotNull && incorrect >= firstIncorrect))
                continue;

            logger.Write(LogLevel.Debug, $"Incorrect frame {incorrect} reported by queue {i}");
            RollbackFrames = new(Math.Max(RollbackFrames.FrameCount, incorrect.Number), options.FramesPerSecond);
            firstIncorrect = incorrect;
        }

        if (firstIncorrect.IsNull)
        {
            logger.Write(LogLevel.Debug, "Prediction ok.  proceeding.");
            seekTo = default;
            RollbackFrames = FrameSpan.Zero;
            return true;
        }

        seekTo = firstIncorrect;
        return false;
    }

    public void SetFrameDelay(PlayerHandle player, int delay) => inputQueues[player.InternalQueue].FrameDelay = delay;

    void ResetPrediction(in Frame frameNumber)
    {
        for (var i = 0; i < inputQueues.Count; i++)
            inputQueues[i].ResetPrediction(in frameNumber);
    }
}
