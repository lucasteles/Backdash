using nGGPO.Core;
using nGGPO.Data;
using nGGPO.Network;

namespace nGGPO.Input;

sealed class Synchronizer<TState> where TState : struct
{
    readonly SavedState savedState;
    readonly InputQueue[] inputQueues;

    // uint frameCount
    // readonly uint maxPredictionFrames = 0

    Frame lastConfirmedFrame = Frame.Zero;

    public Synchronizer(
        SynchronizerOptions options,
        ILogger logger,
        ConnectionStatuses connections
    )
    {
        savedState = new(options.PredictionFrames + options.PredictionFramesOffset);

        inputQueues = new InputQueue[options.NumberOfPlayers];
        for (var i = 0; i < options.NumberOfPlayers; i++)
            inputQueues[i] = new(options.InputQueueLength, logger);
    }

    void SetLastConfirmedFrame(Frame frame)
    {
        lastConfirmedFrame = frame;
        if (lastConfirmedFrame > Frame.Zero)
            for (var i = 0; i < inputQueues.Length; i++)
            {
                inputQueues[i].DiscardConfirmedFrames(frame - 1);
            }
    }

    public bool AddLocalInput(QueueIndex queue, GameInput input)
    {
        throw new NotImplementedException();
    }

    bool AddRemoteInput(QueueIndex queue, GameInput input)
    {
        throw new NotImplementedException();
    }

    int GetConfirmedInputs(object values, int size, int frame)
    {
        throw new NotImplementedException();
    }

    public Span<int> SynchronizeInputs<TInput>(TInput[] inputs) where TInput : struct
    {
        throw new NotImplementedException();
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

public class SynchronizerOptions
{
    public int PredictionFrames { get; init; } = Max.PredictionFrames;
    public int PredictionFramesOffset { get; init; } = 2;
    public int NumberOfPlayers { get; internal set; }

    public int InputQueueLength { get; init; }
}
