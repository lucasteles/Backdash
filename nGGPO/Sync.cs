using System;
using System.Collections.Generic;
using nGGPO.Network;

namespace nGGPO;

public class Sync
{
    public Sync(IReadOnlyList<UdpConnectStatus> localConnectStatus)
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

    public bool AddLocalInput<TInput>(int queue, GameInput<TInput> input) where TInput : struct
    {
        throw new NotImplementedException();
    }

    public int[] SynchronizeInputs<TInput>(TInput[] inputs) where TInput : struct
    {
        throw new NotImplementedException();
    }
}