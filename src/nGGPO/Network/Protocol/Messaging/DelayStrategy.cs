using nGGPO.Core;

namespace nGGPO.Network.Protocol.Messaging;

interface IDelayStrategy
{
    int Jitter(int sendLatency);
}

sealed class DelayStrategy(IRandomNumberGenerator random) : IDelayStrategy
{
    public int Jitter(int sendLatency)
    {
        var mean = sendLatency * 2 / 3;
        return mean + (random.NextInt() % sendLatency / 3);
    }
}

sealed class GaussianDelayStrategy(Random random) : IDelayStrategy
{
    public int Jitter(int sendLatency)
    {
        var mean = sendLatency / 2;
        var sigma = (sendLatency - mean) / 3;
        var std = random.NextGaussian();
        return (int)Math.Clamp((std * sigma) + mean, 0, sendLatency);
    }
}
