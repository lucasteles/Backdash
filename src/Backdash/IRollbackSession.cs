using Backdash.Data;
using Backdash.Network;

namespace Backdash;

public interface IRollbackSessionInfo
{
    public Frame CurrentFrame { get; }
    public FrameSpan RollbackFrames { get; }
    public int FramesPerSecond { get; }
}

public interface IRollbackSession<TInput> : IRollbackSessionInfo, IDisposable where TInput : struct
{
    int NumberOfPlayers { get; }
    int NumberOfSpectators { get; }

    IReadOnlyCollection<PlayerHandle> GetPlayers();
    IReadOnlyCollection<PlayerHandle> GetSpectators();
    ResultCode AddLocalInput(PlayerHandle player, TInput localInput);
    ResultCode SynchronizeInputs();
    ref readonly SynchronizedInput<TInput> GetInput(in PlayerHandle player);
    ref readonly SynchronizedInput<TInput> GetInput(int index);

    void BeginFrame();
    void AdvanceFrame();
    PlayerConnectionStatus GetPlayerStatus(in PlayerHandle player);
    bool GetNetworkStatus(in PlayerHandle player, ref RollbackNetworkStatus info);
    void SetFrameDelay(PlayerHandle player, int delayInFrames);
}

public interface IRollbackSession<TInput, TState> : IRollbackSession<TInput>
    where TInput : struct
    where TState : IEquatable<TState>
{
    ResultCode AddPlayer(Player player);
    IReadOnlyList<ResultCode> AddPlayers(IReadOnlyList<Player> players);
    void Start(CancellationToken stoppingToken = default);
    Task WaitToStop(CancellationToken stoppingToken = default);

    void SetHandler(IRollbackHandler<TState> handler);
}
