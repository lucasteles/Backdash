using Backdash.Data;
using Backdash.Network;

namespace Backdash;

public interface IRollbackSessionInfo
{
    Frame CurrentFrame { get; }
    bool IsSpectating { get; }
    FrameSpan RollbackFrames { get; }
    FrameSpan FramesBehind { get; }
}

public interface IRollbackSession<TInput> : IRollbackSessionInfo, IDisposable where TInput : struct
{
    int NumberOfPlayers { get; }
    int NumberOfSpectators { get; }
    IReadOnlyCollection<PlayerHandle> GetPlayers();
    IReadOnlyCollection<PlayerHandle> GetSpectators();
    void DisconnectPlayer(in PlayerHandle player);
    ResultCode AddLocalInput(PlayerHandle player, TInput localInput);
    ResultCode SynchronizeInputs();
    ref readonly SynchronizedInput<TInput> GetInput(in PlayerHandle player);
    ref readonly SynchronizedInput<TInput> GetInput(int index);

    public void GetInputs(Span<SynchronizedInput<TInput>> buffer)
    {
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = GetInput(i);
    }

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
