using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Backdash.Core;
using Backdash.Network;
using Backdash.Network.Messages;
using Backdash.Options;
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
    readonly EqualityComparer<TInput> inputComparer;
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
        ConnectionsState localConnections,
        EqualityComparer<TInput>? inputComparer = null
    )
    {
        this.options = options;
        this.logger = logger;
        this.players = players;
        this.stateStore = stateStore;
        this.checksumProvider = checksumProvider;
        this.localConnections = localConnections;
        this.inputComparer = inputComparer ?? EqualityComparer<TInput>.Default;

        inputQueues = new(2);
        endianness = options.GetStateSerializationEndianness();
        stateStore.Initialize(options.TotalSavedFramesAllowed);
    }

    public bool InRollback { get; private set; }
    public Frame CurrentFrame => currentFrame;
    public FrameSpan FramesBehind => new(currentFrame.Number - lastConfirmedFrame.Number);
    public FrameSpan RollbackFrames { get; private set; } = FrameSpan.Zero;

    public void AddQueue(PlayerHandle player) =>
        inputQueues.Add(new(player.QueueIndex, options.InputQueueLength, logger, inputComparer)
        {
            LocalFrameDelay = player.IsLocal() ? Math.Max(options.InputDelayFrames, 0) : 0,
        });

    public void SetLastConfirmedFrame(in Frame frame)
    {
        lastConfirmedFrame = frame;
        if (lastConfirmedFrame.Number <= 0)
            return;

        var discardUntil = frame.Previous();
        var span = CollectionsMarshal.AsSpan(inputQueues);
        ref var current = ref MemoryMarshal.GetReference(span);
        ref var limit = ref Unsafe.Add(ref current, span.Length);
        while (Unsafe.IsAddressLessThan(ref current, ref limit))
        {
            current.DiscardConfirmedFrames(discardUntil);
            current = ref Unsafe.Add(ref current, 1)!;
        }
    }

    void AddInput(in PlayerHandle queue, ref GameInput<TInput> input) =>
        inputQueues[queue.QueueIndex].AddInput(ref input);

    public bool AddLocalInput(in PlayerHandle queue, ref GameInput<TInput> input)
    {
        if (currentFrame.Number >= options.PredictionFrames && FramesBehind.FrameCount >= options.PredictionFrames)
        {
            logger.Write(LogLevel.Warning,
                $"Rejecting input for frame {currentFrame.Number} from emulator: reached prediction barrier");
            return false;
        }

        if (currentFrame.Number == 0)
            SaveCurrentFrame();

        logger.Write(LogLevel.Trace, $"Sending non-delayed local frame {currentFrame.Number} to queue {queue}");

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
        return inputQueues[playerNumber].GetConfirmedInput(in frame, ref confirmed);
    }

    public void SynchronizeInputs(Span<SynchronizedInput<TInput>> syncOutput, Span<TInput> output)
    {
        ThrowIf.Assert(syncOutput.Length >= NumberOfPlayers);
        syncOutput.Clear();

        ReadOnlySpan<ConnectStatus> connections = localConnections;
        var queues = CollectionsMarshal.AsSpan(inputQueues);

        for (var i = 0; i < NumberOfPlayers; i++)
        {
            if (connections[i].Disconnected && currentFrame > connections[i].LastFrame)
            {
                syncOutput[i] = new(default, true);
                output[i] = default;
            }
            else
            {
                queues[i].GetInput(currentFrame, out var input);
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
        ThrowIf.Assert(currentFrame.Number == seekTo.Number);

        // Advance frame by frame (stuffing notifications back to the master).
        ResetPrediction(in currentFrame);
        for (var i = 0; i < rollbackCount; i++)
        {
            logger.Write(LogLevel.Debug, $"[Begin Frame {currentFrame}](rollback)");
            Callbacks.AdvanceFrame();
        }

        ThrowIf.Assert(currentFrame == localCurrentFrame);
        InRollback = false;
    }

    public bool TryLoadFrame(in Frame frame)
    {
        // find the frame in question
        if (frame.Number == currentFrame.Number)
        {
            logger.Write(LogLevel.Trace, "Skipping NOP");
            return true;
        }

        if (!stateStore.TryLoad(in frame, out var savedFrame))
            return false;

        logger.Write(LogLevel.Information,
            $"* Loading frame info {savedFrame.Frame} (checksum: {savedFrame.Checksum:x8})");

        var offset = 0;
        BinaryBufferReader reader = new(savedFrame.GameState.WrittenSpan, ref offset, endianness);

        Callbacks.LoadState(in frame, in reader);

        // Reset frame count and the head of the state ring-buffer to point in
        // advance of the current frame (as if we had just finished executing it).
        currentFrame = savedFrame.Frame;
        return true;
    }

    public void LoadFrame(in Frame frame)
    {
        if (!TryLoadFrame(in frame))
            throw new NetcodeException($"Save state not found for frame {frame.Number}");
    }

    public SavedFrame GetLastSavedFrame() => stateStore.Last();

    public void SaveCurrentFrame()
    {
        ref var nextState = ref stateStore.Next();

        BinaryBufferWriter writer = new(nextState.GameState, endianness);
        Callbacks.SaveState(in currentFrame, in writer);
        nextState.Frame = currentFrame;
        nextState.Checksum = checksumProvider.Compute(nextState.GameState.WrittenSpan);

        stateStore.Advance();
        logger.Write(LogLevel.Trace, $"sync: saved frame {nextState.Frame} (checksum: {nextState.Checksum:x8})");
    }

    bool CheckSimulationConsistency(out Frame seekTo)
    {
        var firstIncorrect = Frame.Null;

        var span = CollectionsMarshal.AsSpan(inputQueues);
        ref var current = ref MemoryMarshal.GetReference(span);
        ref var limit = ref Unsafe.Add(ref current, span.Length);
        while (Unsafe.IsAddressLessThan(ref current, ref limit))
        {
            var incorrect = current.FirstIncorrectFrame;
            if (incorrect.IsNull || (!firstIncorrect.IsNull && incorrect.Number >= firstIncorrect.Number))
            {
                current = ref Unsafe.Add(ref current, 1)!;
                continue;
            }

            logger.Write(LogLevel.Information,
                $"Incorrect frame {incorrect.Number} reported by queue {current.QueueId}");
            RollbackFrames = new(Math.Max(RollbackFrames.FrameCount, incorrect.Number));
            firstIncorrect = incorrect;

            current = ref Unsafe.Add(ref current, 1)!;
        }

        if (firstIncorrect.IsNull)
        {
            logger.Write(LogLevel.Trace, "Prediction OK, proceeding");
            seekTo = default;
            RollbackFrames = FrameSpan.Zero;
            return true;
        }

        seekTo = firstIncorrect;
        return false;
    }

    public void SetFrameDelay(PlayerHandle player, int delay) =>
        inputQueues[player.QueueIndex].LocalFrameDelay = Math.Max(delay, 0);

    void ResetPrediction(in Frame frameNumber)
    {
        var span = CollectionsMarshal.AsSpan(inputQueues);
        ref var current = ref MemoryMarshal.GetReference(span);
        ref var limit = ref Unsafe.Add(ref current, span.Length);
        while (Unsafe.IsAddressLessThan(ref current, ref limit))
        {
            current.ResetPrediction(in frameNumber);
            current = ref Unsafe.Add(ref current, 1)!;
        }
    }
}
