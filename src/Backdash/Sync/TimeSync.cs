using Backdash.Core;
using Backdash.Network.Protocol;
using Backdash.Sync.Input;
namespace Backdash.Sync;
public sealed class TimeSyncOptions
{
    public int FrameWindowSize { get; init; } = 40;
    public int MinUniqueFrames { get; init; } = 10;
    public int MinFrameAdvantage { get; init; } = 3;
    public int MaxFrameAdvantage { get; init; } = 9;
}
interface ITimeSync<TInput> where TInput : struct
{
    void AdvanceFrame(in GameInput<TInput> input, in ProtocolState.AdvantageState state);
    int RecommendFrameWaitDuration(bool requireIdleInput);
}
static file class TimeSyncCounter
{
    static int counter;
    public static int Increment() => counter++;
}
sealed class TimeSync<TInput>(
    TimeSyncOptions options,
    Logger logger
) : ITimeSync<TInput>
    where TInput : struct
{
    readonly int minFrameAdvantage = options.MinFrameAdvantage;
    readonly int maxFrameAdvantage = options.MaxFrameAdvantage;
    readonly int[] local = new int[options.FrameWindowSize];
    readonly int[] remote = new int[options.FrameWindowSize];
    readonly GameInput<TInput>[] lastInputs = new GameInput<TInput>[options.MinUniqueFrames];
    public void AdvanceFrame(in GameInput<TInput> input, int advantage, int remoteAdvantage)
    {
        // Remember the last frame and frame advantage
        lastInputs[input.Frame.Number % lastInputs.Length] = input;
        local[input.Frame.Number % local.Length] = advantage;
        remote[input.Frame.Number % remote.Length] = remoteAdvantage;
    }
    public void AdvanceFrame(in GameInput<TInput> input, in ProtocolState.AdvantageState state) =>
        AdvanceFrame(in input, state.LocalFrameAdvantage.FrameCount, state.RemoteFrameAdvantage.FrameCount);
    public int RecommendFrameWaitDuration(bool requireIdleInput)
    {
        // Average our local and remote frame advantages
        var localAdvantage = local.Average();
        var remoteAdvantage = remote.Average();
        var iteration = TimeSyncCounter.Increment();
        // See if someone should take action.  The person furthest ahead
        // needs to slow down so the other user can catch up.
        // Only do this if both clients agree on who's ahead!!
        if (localAdvantage >= remoteAdvantage)
            return 0;
        // Both clients agree that we're the one ahead.  Split
        // the difference between the two to figure out how long to
        // sleep for.
        var sleepFrames = (int)(((remoteAdvantage - localAdvantage) / 2) + 0.5f);
        logger.Write(LogLevel.Trace, $"iteration {iteration}:  sleep frames is {sleepFrames}");
        // Some things just aren't worth correcting for.  Make sure
        // the difference is relevant before proceeding.
        if (sleepFrames < minFrameAdvantage)
            return 0;
        // Make sure our input had been "idle enough" before recommending
        // a sleep.  This tries to make the emulator sleep while the
        // user's input isn't sweeping in arcs (e.g. fireball motions in
        // Street Fighter), which could cause the player to miss moves.
        if (requireIdleInput)
        {
            SpinWait sw = new();
            for (var i = 1; i < lastInputs.Length; i++)
            {
                if (EqualityComparer<TInput>.Default.Equals(lastInputs[i].Data, lastInputs[0].Data))
                {
                    sw.SpinOnce();
                    continue;
                }
                logger.Write(LogLevel.Debug,
                    $"iteration {iteration}:  rejecting due to input stuff at position {i}!");
                return 0;
            }
        }
        var recommendation = Math.Min(sleepFrames, maxFrameAdvantage);
        logger.Write(LogLevel.Information,
            $"time sync: recommending sleep: {recommendation}, total:{sleepFrames}, max:{maxFrameAdvantage}");
        // Success!!! Recommend the number of frames to sleep and adjust
        return recommendation;
    }
}
