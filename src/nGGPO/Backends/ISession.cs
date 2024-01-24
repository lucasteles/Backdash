namespace nGGPO.Backends;

public interface IRollbackSession<in TInput, TGameState> : IDisposable
    where TInput : struct
    where TGameState : struct
{
    public ResultCode AddPlayer(Player player);
    public ResultCode SetFrameDelay(Player player, int delayInFrames);
    public ValueTask<ResultCode> AddLocalInput(PlayerHandle player, TInput localInput);
    public ResultCode SynchronizeInputs(params TInput[] inputs);
    public ResultCode SynchronizeInputs(out int[] disconnectFlags, params TInput[] inputs);

    // LATER: remove this
    public TGameState? GetState() => null;
}
