using Backdash.Core;

namespace Backdash.Network.Protocol.Messaging;

interface IDelayStrategy
{
    TimeSpan Jitter(int sendLatency);
}

sealed class DelayStrategy(IRandomNumberGenerator random) : IDelayStrategy
{
    public TimeSpan Jitter(int sendLatency)
    {
        var mean = sendLatency * 2 / 3;
        var ms = mean + (random.NextInt() % sendLatency / 3);
        return TimeSpan.FromMilliseconds(ms);
    }
}

sealed class GaussianDelayStrategy(Random random) : IDelayStrategy
{
    public TimeSpan Jitter(int sendLatency)
    {
        var mean = sendLatency / 2;
        var sigma = (sendLatency - mean) / 3;
        var std = random.NextGaussian();
        var ms = (int)Math.Clamp((std * sigma) + mean, 0, sendLatency);
        return TimeSpan.FromMilliseconds(ms);
    }
}
