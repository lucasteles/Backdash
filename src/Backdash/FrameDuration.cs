using Backdash.Data;

namespace Backdash;

public static class FrameDuration
{
    public const int SixtyFrames = 60;

    public static double InSeconds(int frame, int fps = SixtyFrames) => frame / (double)fps;

    public static Frame FromSeconds(double seconds, int fps = SixtyFrames) => new((int)(seconds * fps));

    public static TimeSpan InTimeSpan(int frame, int fps = SixtyFrames) =>
        TimeSpan.FromSeconds(InSeconds(frame, fps));

    public static Frame FromTimeSpan(TimeSpan time, int fps = SixtyFrames) =>
        FromSeconds(time.TotalSeconds, fps);


    public static double InMilliseconds(int frame, int fps = SixtyFrames) => InSeconds(frame, fps) * 1000.0;

    public static Frame FromMilliseconds(double milliseconds, int fps = SixtyFrames) =>
        FromSeconds(milliseconds / 1000, fps);
}
