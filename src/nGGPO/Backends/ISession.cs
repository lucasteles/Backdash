namespace nGGPO.Backends;

public interface IRollbackSession<in TInput> : IAsyncDisposable
    where TInput : struct
{
    ResultCode AddPlayer(Player player);
    ResultCode SetFrameDelay(Player player, int delayInFrames);
    ValueTask<ResultCode> AddLocalInput(PlayerId player, TInput localInput, CancellationToken stoppingToken = default);
    ResultCode SynchronizeInputs(params TInput[] inputs);
    ResultCode SynchronizeInputs(out int[] disconnectFlags, params TInput[] inputs);

    Task Start(CancellationToken ct = default);
}
