namespace nGGPO.Backends;

public interface IRollbackSession<in TInput, TGameState> : IDisposable
    where TInput : struct
    where TGameState : struct
{
    public ErrorCode AddPlayer(Player player);
    public ErrorCode SetFrameDelay(Player player, int frameDelay);
    public Task<ErrorCode> AddLocalInput(PlayerHandle player, TInput input);
    public ErrorCode SynchronizeInputs(params TInput[] inputs);
    public ErrorCode SynchronizeInputs(out int[] disconnectFlags, params TInput[] inputs);

    // LATER: remove this
    public TGameState? GetState() => null;
}
