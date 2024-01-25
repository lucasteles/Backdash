using nGGPO.Data;
using nGGPO.Network;
using nGGPO.Utils;

namespace nGGPO.Input;

static class Synchronizer
{
    public struct SavedFrame<TGameState>() where TGameState : struct
    {
        public int Frame = -1;
        public TGameState Buf = default;
        public int Cbuf = 0;
        public int Checksum = 0;
    }

    public class SavedState<TGameState> where TGameState : struct
    {
        public int Head = 0;
        public SavedFrame<TGameState>[] Frames = new SavedFrame<TGameState>[Max.PredictionFrames + 2];
    }
}

sealed class Synchronizer<TGameState>(Connections connectStatus)
    where TGameState : struct
{
    readonly Synchronizer.SavedState<TGameState> savedState = new();

    Frame lastConfirmedFrame = Frame.Null;
    uint frameCount;
    readonly uint maxPredictionFrames = 0;

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
}
