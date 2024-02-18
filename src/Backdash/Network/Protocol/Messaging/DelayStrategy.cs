using Backdash.Core;

namespace Backdash.Network.Protocol.Messaging;

interface IDelayStrategy
{
    TimeSpan Jitter(TimeSpan sendLatency);
}

sealed class DefaultDelayStrategy(IRandomNumberGenerator random) : IDelayStrategy
{
    public TimeSpan Jitter(TimeSpan sendLatency)
    {
        var latency = sendLatency.TotalMilliseconds;
        var mean = latency * 2 / 3;
        var ms = mean + (random.NextInt() % latency / 3);
        return TimeSpan.FromMilliseconds(ms);
    }
}

sealed class GaussianDelayStrategy(IRandomNumberGenerator random) : IDelayStrategy
{
    public TimeSpan Jitter(TimeSpan sendLatency)
    {
        var latency = sendLatency.TotalMilliseconds;
        var mean = latency / 2;
        var sigma = (latency - mean) / 3;
        var std = random.NextGaussian();
        var ms = (int)Math.Clamp((std * sigma) + mean, 0, latency);
        return TimeSpan.FromMilliseconds(ms);
    }
}
