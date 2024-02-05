using Backdash.Input;

namespace Backdash.Backends;

public interface IRollbackSession<TInput> : IAsyncDisposable
    where TInput : struct
{
    Task Start(CancellationToken ct = default);
    ResultCode SetFrameDelay(Player player, int delayInFrames);
    SynchronizeResult SynchronizeInputs(Span<TInput> inputs);
    ValueTask<ResultCode> AddPlayer(Player player, CancellationToken ct);

    ValueTask<ResultCode> AddLocalInput(
        PlayerIndex player, TInput localInput, CancellationToken stoppingToken = default
    );

    ResultCode TryAddLocalInput(PlayerIndex player, TInput localInput);
}
