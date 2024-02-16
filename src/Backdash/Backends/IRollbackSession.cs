using Backdash.Network;

namespace Backdash.Backends;

public interface IRollbackSession<TInput, TState> : IDisposable
    where TInput : struct
    where TState : notnull
{
    ResultCode SynchronizeInputs(Span<TInput> inputs);
    ResultCode AddPlayer(Player player);
    ResultCode AddLocalInput(PlayerHandle player, TInput localInput);

    void BeginFrame();
    void AdvanceFrame();
    bool IsConnected(in PlayerHandle player);
    bool GetInfo(in PlayerHandle player, ref RollbackSessionInfo info);
    void SetHandler(IRollbackHandler<TState> handler);
    void SetFrameDelay(PlayerHandle player, int delayInFrames);
    void Start(CancellationToken stoppingToken = default);
    Task WaitToStop(CancellationToken stoppingToken = default);
}
