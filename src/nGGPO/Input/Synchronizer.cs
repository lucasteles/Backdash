using nGGPO.Core;
using nGGPO.Data;
using nGGPO.Network;

namespace nGGPO.Input;

sealed class Synchronizer<TState>(
    SynchronizerOptions options,
    ConnectionStatuses connections
)
    where TState : struct
{
    readonly SavedState savedState = new(options.PredictionFrames + options.PredictionFramesOffset);

    // Frame lastConfirmedFrame = Frame.Null
    // uint frameCount
    // readonly uint maxPredictionFrames = 0

    public void SetFrameDelay(QueueIndex queue, int delay)
    {
        throw new NotImplementedException();
    }

    public bool InRollback()
    {
        throw new NotImplementedException();
    }

    public bool AddLocalInput(QueueIndex queue, GameInput input)
    {
        throw new NotImplementedException();
    }

    public int[] SynchronizeInputs<TInput>(TInput[] inputs) where TInput : struct
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
}
