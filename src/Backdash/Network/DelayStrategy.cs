using Backdash.Core;

namespace Backdash.Network;

/// <summary>
///     Jitter delay strategy
/// </summary>
public enum DelayStrategy
{
    /// <summary>Constant delay</summary>
    Constant,

    /// <summary>Random gaussian delay</summary>
    Gaussian,

    /// <summary>Random continuous delay</summary>
    ContinuousUniform,
}

interface IDelayStrategy
{
    TimeSpan Jitter(TimeSpan sendLatency);
}

static class DelayStrategyFactory
{
    public static IDelayStrategy Create(IRandomNumberGenerator random, DelayStrategy strategy) => strategy switch
    {
        DelayStrategy.Constant => new ConstantDelayStrategy(),
        DelayStrategy.Gaussian => new GaussianDelayStrategy(random),
        DelayStrategy.ContinuousUniform => new UniformDelayStrategy(random),
        _ => throw new ArgumentOutOfRangeException(nameof(strategy), strategy, null),
    };
}

sealed class ConstantDelayStrategy : IDelayStrategy
{
    public TimeSpan Jitter(TimeSpan sendLatency) => sendLatency;
}

sealed class UniformDelayStrategy(IRandomNumberGenerator random) : IDelayStrategy
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
