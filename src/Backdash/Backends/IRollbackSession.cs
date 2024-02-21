using Backdash.Network;

namespace Backdash.Backends;

public interface IRollbackSession<TInput> : IDisposable where TInput : struct
{
    int NumberOfPlayers { get; }

    int NumberOfSpectators { get; }

    ResultCode AddPlayer(Player player);
    void AddPlayers(IEnumerable<Player> players);

    IReadOnlyCollection<PlayerHandle> GetPlayers();
    IReadOnlyCollection<PlayerHandle> GetSpectators();
    ResultCode AddLocalInput(PlayerHandle player, TInput localInput);
    ResultCode SynchronizeInputs(Span<TInput> inputs);
    void BeginFrame();
    void AdvanceFrame();
    bool IsConnected(in PlayerHandle player);
    bool GetInfo(in PlayerHandle player, ref RollbackSessionInfo info);
    void SetFrameDelay(PlayerHandle player, int delayInFrames);
    void Start(CancellationToken stoppingToken = default);
    Task WaitToStop(CancellationToken stoppingToken = default);
}

public interface IRollbackSession<TInput, TState> : IRollbackSession<TInput>
    where TInput : struct
    where TState : IEquatable<TState>
{
    void SetHandler(IRollbackHandler<TState> handler);
}
