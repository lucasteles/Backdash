namespace nGGPO.Backends;

public interface IRollbackSession<in TInput> : IAsyncDisposable
    where TInput : struct
{
    Task Start(CancellationToken ct = default);
    ResultCode SetFrameDelay(Player player, int delayInFrames);
    ResultCode SynchronizeInputs(params TInput[] inputs);
    ResultCode SynchronizeInputs(out Span<int> disconnectFlags, params TInput[] inputs);
    ValueTask<ResultCode> AddPlayer(Player player, CancellationToken ct);

    ValueTask<ResultCode> AddLocalInput(
        PlayerId player, TInput localInput, CancellationToken stoppingToken = default
    );

    ResultCode TryAddLocalInput(PlayerId player, TInput localInput);
}
