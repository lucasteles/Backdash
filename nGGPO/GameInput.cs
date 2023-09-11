namespace nGGPO;

public static class GameInput
{
    public const int NullFrame = -1;
}

public record GameInput<TInput>(TInput Input, int Frame = GameInput.NullFrame)
    where TInput : struct;