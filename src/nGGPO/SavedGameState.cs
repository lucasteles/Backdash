namespace nGGPO;

public readonly struct SavedGameState<TState>(TState state, int checksum = 0)
    where TState : struct
{
    public readonly TState State = state;
    public readonly int Checksum = checksum;
}
