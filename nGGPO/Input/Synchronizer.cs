using nGGPO.Network.Messages;
using nGGPO.Utils;

namespace nGGPO.Input;

class Synchronizer<TGameState> where TGameState : struct
{
    public struct SavedFrame
    {
        public TGameState Buf;
        public int Cbuf;
        public int Frame;
        public int Checksum;

        public SavedFrame()
        {
            Buf = default;
            Cbuf = 0;
            Checksum = 0;
            Frame = -1;
        }
    };

    public struct SavedState
    {
        public SavedFrame[] Frames;
        public int Head;

        public SavedState()
        {
            Frames = new SavedFrame[Max.PredictionFrames + 2];
            Head = 0;
        }
    };

    readonly IReadOnlyList<ConnectStatus> connectStatus;
    int frameCount;
    int lastConfirmedFrame = -1;
    int maxPredictionFrames = 0;
    SavedState? savedstate;

    public Synchronizer(IReadOnlyList<ConnectStatus> connectStatus) =>
        this.connectStatus = connectStatus;

    public void SetFrameDelay(int queue, int delay)
    {
        throw new NotImplementedException();
    }

    public bool InRollback()
    {
        throw new NotImplementedException();
    }

    public bool AddLocalInput(int queue, GameInput input)
    {
        throw new NotImplementedException();
    }

    public int[] SynchronizeInputs<TInput>(TInput[] inputs) where TInput : struct
    {
        throw new NotImplementedException();
    }
}
