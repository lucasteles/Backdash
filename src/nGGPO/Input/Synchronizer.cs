using nGGPO.Core;
using nGGPO.Data;
using nGGPO.Network;

namespace nGGPO.Input;

sealed class Synchronizer<TState> where TState : struct
{
    readonly RollbackOptions options;
    readonly ILogger logger;
    readonly ConnectionStatuses localConnections;
    readonly SavedState savedState;
    readonly InputQueue[] inputQueues;

    bool _rollingback;
    Frame _last_confirmed_frame;
    Frame _framecount;
    int _max_prediction_frames;

    Frame lastConfirmedFrame = Frame.Zero;

    public Synchronizer(
        RollbackOptions options,
        ILogger logger,
        ConnectionStatuses localConnections
    )
    {
        Tracer.Assert(options.InputSize > 0);

        this.options = options;
        this.logger = logger;
        this.localConnections = localConnections;

        savedState = new(options.PredictionFrames + options.PredictionFramesOffset);
        inputQueues = new InputQueue[options.NumberOfPlayers];
        for (var i = 0; i < options.NumberOfPlayers; i++)
            inputQueues[i] = new(options.InputSize, options.InputQueueLength, logger);
    }

    void SetLastConfirmedFrame(Frame frame)
    {
        lastConfirmedFrame = frame;

        if (lastConfirmedFrame > Frame.Zero)
            for (var i = 0; i < inputQueues.Length; i++)
                inputQueues[i].DiscardConfirmedFrames(frame.Previous);
    }

    void AddInput(in QueueIndex queue, ref GameInput input) => inputQueues[queue.Value].AddInput(ref input);

    public bool AddLocalInput(QueueIndex queue, GameInput input)
    {
        var framesBehind = _framecount - _last_confirmed_frame;
        if (_framecount >= _max_prediction_frames && framesBehind >= _max_prediction_frames)
        {
            logger.Info($"Rejecting input from emulator: reached prediction barrier.");
            return false;
        }

        if (_framecount == 0)
            SaveCurrentFrame();

        logger.Info($"Sending non-delayed local frame {_framecount} to queue {queue}.");
        input.Frame = _framecount;
        AddInput(in queue, ref input);

        return true;
    }

    public void AddRemoteInput(QueueIndex queue, GameInput input) => AddInput(in queue, ref input);

    // TODO: this should actually return the real deserialized inputs
    public DisconnectFlags GetConfirmedInputs(Frame frame, Span<byte> output)
    {
        Tracer.Assert(output.Length >= options.NumberOfPlayers * options.InputSize);

        var disconnectFlags = 0;
        output.Clear();

        var input = GameInput.OfSize(options.InputSize);
        for (var i = 0; i < options.NumberOfPlayers; i++)
        {
            if (localConnections[i].Disconnected && frame > localConnections[i].LastFrame)
                disconnectFlags |= 1 << i;
            else
                inputQueues[i].GetConfirmedInput(frame, ref input);

            var playerInput = output.Slice(i * options.InputSize, options.InputSize);
            input.ForPlayer(0, playerInput);
            input.Erase();
        }

        return new(disconnectFlags);
    }

    // TODO: this should actually return the real deserialized inputs
    public DisconnectFlags SynchronizeInputs(Span<byte> output)
    {
        var disconnectFlags = 0;
        Tracer.Assert(output.Length >= options.NumberOfPlayers * options.InputSize);
        output.Clear();

        var input = GameInput.OfSize(options.InputSize);
        for (int i = 0; i < options.NumberOfPlayers; i++)
        {
            if (localConnections[i].Disconnected && _framecount > localConnections[i].LastFrame)
                disconnectFlags |= 1 << i;
            else
                inputQueues[i].GetInput(_framecount, out input);

            var playerInput = output.Slice(i * options.InputSize, options.InputSize);
            input.ForPlayer(0, playerInput);
            input.Erase();
        }

        return new(disconnectFlags);
    }

    void CheckSimulation(int timeout)
    {
        throw new NotImplementedException();
    }

    void IncrementFrame()
    {
        throw new NotImplementedException();
    }

    void AdjustSimulation(int seekTo)
    {
        throw new NotImplementedException();
    }

    void LoadFrame(int frame)
    {
        throw new NotImplementedException();
    }

    void SaveCurrentFrame()
    {
        throw new NotImplementedException();
    }

    public void SetFrameDelay(QueueIndex queue, int delay)
    {
        throw new NotImplementedException();
    }

    public bool InRollback()
    {
        throw new NotImplementedException();
    }

    SavedFrame GetLastSavedFrame()
    {
        throw new NotImplementedException();
    }

    int FindSavedFrameIndex(int frame)
    {
        throw new NotImplementedException();
    }

    bool CheckSimulationConsistency(ref int seekTo)
    {
        throw new NotImplementedException();
    }

    void SetFrameDelay(int queue, int delay)
    {
        throw new NotImplementedException();
    }

    bool GetEvent(in GameInput e)
    {
        throw new NotImplementedException();
    }

    public struct SavedFrame()
    {
        public Frame Frame = Frame.Null;
        public TState Buf = default;
        public int Cbuf = 0;
        public int Checksum = 0;
    }

    public class SavedState(int predictionSize)
    {
        public int Head { get; set; }
        public SavedFrame[] Frames = new SavedFrame[predictionSize];
    }
}
