using nGGPO.Network.Protocol.Internal;

namespace nGGPO.Input;

public sealed class TimeSyncOptions
{
    public int FrameWindowSize { get; init; } = 40;
    public int MinUniqueFrames { get; init; } = 10;
    public int MinFrameAdvantage { get; init; } = 3;
    public int MaxFrameAdvantage { get; init; } = 9;
}

interface ITimeSync
{
    void AdvanceFrame(in GameInput input, ProtocolState.AdvantageState state);

    int RecommendFrameWaitDuration(bool requireIdleInput);
}

sealed class TimeSync(
    TimeSyncOptions options,
    ILogger logger
) : ITimeSync
{
    static int counter;

    readonly int minFrameAdvantage = options.MinFrameAdvantage;
    readonly int maxFrameAdvantage = options.MaxFrameAdvantage;

    readonly int[] local = new int[options.FrameWindowSize];
    readonly int[] remote = new int[options.FrameWindowSize];
    readonly GameInput[] lastInputs = new GameInput[options.MinUniqueFrames];

    public void AdvanceFrame(in GameInput input, int advantage, int remoteAdvantage)
    {
        // Remember the last frame and frame advantage
        lastInputs[input.Frame % lastInputs.Length] = input;
        local[input.Frame % local.Length] = advantage;
        remote[input.Frame % remote.Length] = remoteAdvantage;
    }

    public void AdvanceFrame(in GameInput input, ProtocolState.AdvantageState state) =>
        AdvanceFrame(in input, state.LocalFrameAdvantage, state.RemoteFrameAdvantage);

    public int RecommendFrameWaitDuration(bool requireIdleInput)
    {
        // Average our local and remote frame advantages
        int i, sum = 0;
        for (i = 0; i < local.Length; i++)
            sum += local[i];

        var localAdvantage = sum / (float)local.Length;

        sum = 0;
        for (i = 0; i < remote.Length; i++)
            sum += remote[i];

        var remoteAdvantage = sum / (float)remote.Length;
        Interlocked.Increment(ref counter);

        // See if someone should take action.  The person furthest ahead
        // needs to slow down so the other user can catch up.
        // Only do this if both clients agree on who's ahead!!
        if (localAdvantage >= remoteAdvantage)
            return 0;

        // Both clients agree that we're the one ahead.  Split
        // the difference between the two to figure out how long to
        // sleep for.
        var sleepFrames = (int)(((remoteAdvantage - localAdvantage) / 2) + 0.5f);

        logger.Info($"iteration {counter}:  sleep frames is {sleepFrames}");

        // Some things just aren't worth correcting for.  Make sure
        // the difference is relevant before proceeding.
        if (sleepFrames < minFrameAdvantage)
            return 0;

        // Make sure our input had been "idle enough" before recommending
        // a sleep.  This tries to make the emulator sleep while the
        // user's input isn't sweeping in arcs (e.g. fireball motions in
        // Street Fighter), which could cause the player to miss moves.
        if (!requireIdleInput)
            return Math.Min(sleepFrames, maxFrameAdvantage);

        for (i = 1; i < lastInputs.Length; i++)
        {
            if (lastInputs[i].Equals(lastInputs[0], true, logger)) continue;
            logger.Info($"iteration {counter}:  rejecting due to input stuff at position {i}...!!!");

            return 0;
        }

        // Success!!! Recommend the number of frames to sleep and adjust
        return Math.Min(sleepFrames, maxFrameAdvantage);
    }
}
