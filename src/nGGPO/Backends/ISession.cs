namespace nGGPO.Backends;

public interface IRollbackSession<in TInput, TGameState> : IDisposable
    where TInput : struct
    where TGameState : struct
{
    ResultCode AddPlayer(Player player);
    ResultCode SetFrameDelay(Player player, int delayInFrames);
    ValueTask<ResultCode> AddLocalInput(PlayerId player, TInput localInput, CancellationToken stoppingToken = default);
    ResultCode SynchronizeInputs(params TInput[] inputs);
    ResultCode SynchronizeInputs(out int[] disconnectFlags, params TInput[] inputs);

    // LATER: remove this
    TGameState? GetState() => null;

    Task Start(CancellationToken ct = default);
}
