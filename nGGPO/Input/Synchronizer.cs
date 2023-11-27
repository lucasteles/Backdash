using nGGPO.Network.Messages;

namespace nGGPO.Input;

class Synchronizer
{
    public Synchronizer(IReadOnlyList<ConnectStatus> localConnectStatus)
    {
        throw new NotImplementedException();
    }

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