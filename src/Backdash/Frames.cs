namespace Backdash;

public static class Frames
{
    public const int DefaultFps = 60;

    public static double ToMilliseconds(int frame, int fps = DefaultFps) =>
        frame * 1000.0 / fps;

    public static TimeSpan ToTimeSpan(int frame, int fps = DefaultFps) =>
        TimeSpan.FromMilliseconds(ToMilliseconds(frame, fps));
}
