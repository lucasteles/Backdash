using System.Diagnostics;
using Backdash.Core;
using Backdash.Data;
using Backdash.Network;
using Backdash.Serialization;
using Backdash.Synchronizing.Input.Confirmed;
using Backdash.Synchronizing.State;

namespace Backdash.Synchronizing.Input;

sealed class Synchronizer<TInput> where TInput : unmanaged
{
    readonly NetcodeOptions options;
    readonly Logger logger;
    readonly IReadOnlyCollection<PlayerHandle> players;
    readonly IStateStore stateStore;
    readonly IChecksumProvider checksumProvider;
    readonly ConnectionsState localConnections;
    readonly List<InputQueue<TInput>> inputQueues;
    public required INetcodeSessionHandler Callbacks { get; internal set; }
    Frame currentFrame = Frame.Zero;
    Frame lastConfirmedFrame = Frame.Zero;
    int NumberOfPlayers => players.Count;

    readonly Endianness endianness;

    public Synchronizer(
        NetcodeOptions options,
        Logger logger,
        IReadOnlyCollection<PlayerHandle> players,
        IStateStore stateStore,
        IChecksumProvider checksumProvider,
        ConnectionsState localConnections
    )
    {
        this.options = options;
        this.logger = logger;
        this.players = players;
        this.stateStore = stateStore;
        this.checksumProvider = checksumProvider;
        this.localConnections = localConnections;

        endianness = Platform.GetEndianness(options.UseNetworkEndianness);
        stateStore.Initialize(options.PredictionFrames + options.PredictionFramesOffset);
        inputQueues = new(2);
    }

    public bool InRollback { get; private set; }
    public Frame CurrentFrame => currentFrame;
    public FrameSpan FramesBehind => new(currentFrame.Number - lastConfirmedFrame.Number);
    public FrameSpan RollbackFrames { get; private set; } = FrameSpan.Zero;

    public void AddQueue(PlayerHandle player) =>
        inputQueues.Add(new(player.InternalQueue, options.InputQueueLength, logger)
        {
            LocalFrameDelay = player.IsLocal() ? Math.Max(options.FrameDelay, 0) : 0,
        });

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

        logger.Write(LogLevel.Trace, $"Sending non-delayed local frame {currentFrame} to queue {queue}");

        input.Frame = currentFrame;
        AddInput(in queue, ref input);
        return true;
    }

    public void AddRemoteInput(in PlayerHandle player, GameInput<TInput> input) => AddInput(in player, ref input);

    public bool GetConfirmedInputGroup(in Frame frame, ref GameInput<ConfirmedInputs<TInput>> confirmed)
    {
        confirmed.Data.Count = (byte)NumberOfPlayers;
        confirmed.Frame = frame;
        GameInput<TInput> current = new();
        for (var playerNumber = 0; playerNumber < NumberOfPlayers; playerNumber++)
        {
            if (!GetConfirmedInput(in frame, playerNumber, ref current))
                return false;
            confirmed.Data.Inputs[playerNumber] = current.Data;
        }

        return true;
    }

    public bool GetConfirmedInput(in Frame frame, int playerNumber, ref GameInput<TInput> confirmed)
    {
        if (localConnections[playerNumber].Disconnected && frame > localConnections[playerNumber].LastFrame)
            return false;
        confirmed.Frame = frame;
        inputQueues[playerNumber].GetConfirmedInput(in frame, ref confirmed);
        return true;
    }

    public void SynchronizeInputs(Span<SynchronizedInput<TInput>> syncOutput, Span<TInput> output)
    {
        Trace.Assert(syncOutput.Length >= NumberOfPlayers);
        syncOutput.Clear();
        for (var i = 0; i < NumberOfPlayers; i++)
        {
            if (localConnections[i].Disconnected && currentFrame > localConnections[i].LastFrame)
            {
                syncOutput[i] = new(default, true);
                output[i] = default;
            }
            else
            {
                inputQueues[i].GetInput(currentFrame, out var input);
                syncOutput[i] = new(input.Data, false);
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

        var savedFrame = stateStore.Load(frame);
        logger.Write(LogLevel.Information,
            $"* Loading frame info {savedFrame.Frame} (checksum: {savedFrame.Checksum})");

        var offset = 0;
        BinaryBufferReader reader = new(savedFrame.GameState.WrittenSpan, ref offset, endianness);


        Callbacks.LoadState(in frame, in reader);

        // Reset frame count and the head of the state ring-buffer to point in
        // advance of the current frame (as if we had just finished executing it).
        currentFrame = savedFrame.Frame;
    }

    public SavedFrame GetLastSavedFrame() => stateStore.Last();

    public void SaveCurrentFrame()
    {
        ref var nextState = ref stateStore.GetCurrent();

        BinaryBufferWriter writer = new(nextState.GameState, endianness);
        Callbacks.SaveState(in currentFrame, in writer);
        nextState.Frame = currentFrame;
        nextState.Checksum = checksumProvider.Compute(nextState.GameState.WrittenSpan);

        stateStore.Advance();
        logger.Write(LogLevel.Trace, $"sync: saved frame {nextState.Frame} (checksum: {nextState.Checksum}).");
    }

    bool CheckSimulationConsistency(out Frame seekTo)
    {
        var firstIncorrect = Frame.Null;
        for (var i = 0; i < NumberOfPlayers; i++)
        {
            var incorrect = inputQueues[i].FirstIncorrectFrame;
            if (incorrect.IsNull || (firstIncorrect.IsNotNull && incorrect >= firstIncorrect))
                continue;
            logger.Write(LogLevel.Information, $"Incorrect frame {incorrect} reported by queue {i}");
            RollbackFrames = new(Math.Max(RollbackFrames.FrameCount, incorrect.Number));
            firstIncorrect = incorrect;
        }

        if (firstIncorrect.IsNull)
        {
            logger.Write(LogLevel.Trace, "Prediction ok.  proceeding.");
            seekTo = default;
            RollbackFrames = FrameSpan.Zero;
            return true;
        }

        seekTo = firstIncorrect;
        return false;
    }

    public void SetFrameDelay(PlayerHandle player, int delay) =>
        inputQueues[player.InternalQueue].LocalFrameDelay = Math.Max(delay, 0);

    void ResetPrediction(in Frame frameNumber)
    {
        for (var i = 0; i < inputQueues.Count; i++)
            inputQueues[i].ResetPrediction(in frameNumber);
    }
}
