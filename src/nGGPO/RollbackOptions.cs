using nGGPO.Input;

namespace nGGPO;

public sealed class RollbackOptions
{
    public required int LocalPort { get; init; }
    public required int NumberOfPlayers { get; init; }

    public int RecommendationInterval { get; init; } = 240;
    public int DisconnectTimeout { get; init; } = 5000;
    public int DisconnectNotifyStart { get; init; } = 750;
    public int SpectatorOffset { get; init; } = 1000;

    public Random Random { get; init; } = Random.Shared;
    public TimeSyncOptions TimeSync { get; init; } = new();
    public int NetworkDelay { get; init; }

    public LogLevel LogLevel { get; init; } =
#if DEBUG
        LogLevel.Trace;
#else
        LogLevel.Error;
#endif
}
