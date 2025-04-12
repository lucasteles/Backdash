using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

#pragma warning disable S3218, S4136

namespace Backdash;

/// <summary>
///     Frame time helpers
/// </summary>
public static class FrameTime
{
    /// <summary>Return one frame in seconds for 60 FPS</summary>
    public const float One60FpsF = 1f / 60f;

    /// <summary>Return one frame in seconds for 30 FPS</summary>
    public const float One30FpsF = 1f / 30f;

    /// <summary>Return one frame in seconds for 60 FPS</summary>
    public const double One60Fps = 1f / 60f;

    /// <summary>Return one frame in seconds for 30 FPS</summary>
    public const double One30Fps = 1f / 30f;

    /// <summary>
    ///     Default FPS(frames per second)
    ///     <value>60</value>
    /// </summary>
    public const int DefaultFrameRate = 60;

    /// <summary>
    /// Default Instance for <see cref="Fixed"/>
    /// </summary>
    public static readonly Fixed Default = new(DefaultFrameRate);

    /// <summary>
    ///     Current FPS(frames per second) used in <see cref="Default"/>
    ///     <value>60</value>
    /// </summary>
    public static int CurrentFrameRate
    {
        get => Default.FrameRate;
        set => Default.SetFrameRate(value);
    }

    /// <summary>Return one frame in seconds</summary>
    public static double One => Default.One;

    /// <summary>Return one frame in seconds</summary>
    public static TimeSpan Step => Default.Step;

    /// <summary>
    ///     Returns <see cref="double" /> seconds for <paramref name="frameCount" />.
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double GetSeconds(int frameCount) => Default.GetSeconds(frameCount);

    /// <summary>
    ///     Returns <see cref="double" /> seconds for <paramref name="frameCount" />.
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double GetSeconds(double frameCount) => Default.GetSeconds(frameCount);

    /// <summary>
    ///     Returns <see cref="double" /> seconds for <paramref name="frameCount" />.
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double GetMilliseconds(int frameCount) => Default.GetMilliseconds(frameCount);

    /// <summary>
    ///     Returns <see cref="System.TimeSpan" /> for <paramref name="frameCount" />.
    /// </summary>
    public static TimeSpan GetDuration(int frameCount) => Default.GetDuration(frameCount);

    /// <summary>
    ///     Returns the amount of frames for <paramref name="seconds" />.
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double GetTotalFrames(double seconds) => Default.GetTotalFrames(seconds);

    /// <summary>
    ///     Returns the amount of frames for <paramref name="duration" />.
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double GetTotalFrames(TimeSpan duration) => Default.GetTotalFrames(duration);

    /// <summary>
    ///     Returns the amount of frames for <paramref name="milliseconds" />.
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double GetTotalMillisecondFrames(double milliseconds) =>
        Default.GetTotalMillisecondFrames(milliseconds);

    /// <summary>
    ///     Returns the amount of frames for <paramref name="seconds" />.
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetFrames(double seconds) => Default.GetFrames(seconds);

    /// <summary>
    ///     Returns the amount of frames for <paramref name="duration" />.
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetFrames(TimeSpan duration) => Default.GetFrames(duration);

    /// <summary>
    ///     Returns the amount of frames for <paramref name="milliseconds" />.
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetMillisecondFrames(double milliseconds) => Default.GetMillisecondFrames(milliseconds);

    /// <summary>Return one frame in seconds</summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double RateStepSeconds(int fps) => GetSeconds(1, fps);

    /// <summary>Return one frame duration</summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeSpan RateStep(int fps) => GetDuration(1, fps);

    /// <summary>
    ///     Returns <see cref="double" /> seconds for <paramref name="frameCount" /> at specified <paramref name="fps" />.
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double GetSeconds(double frameCount, int fps) => frameCount / fps;

    /// <summary>
    ///     Returns <see cref="double" /> seconds for <paramref name="frameCount" /> at specified <paramref name="fps" />.
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double GetSeconds(int frameCount, int fps) => GetSeconds((double)frameCount, fps);

    /// <summary>
    ///     Returns <see cref="double" /> seconds for <paramref name="frameCount" /> at specified <paramref name="fps" />.
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double GetMilliseconds(int frameCount, int fps) =>
        GetSeconds(frameCount, fps) / 1000.0;

    /// <summary>
    ///     Returns <see cref="System.TimeSpan" /> for <paramref name="frameCount" /> at specified <paramref name="fps" />.
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeSpan GetDuration(int frameCount, int fps) =>
        TimeSpan.FromSeconds(GetSeconds(frameCount, fps));

    /// <summary>
    ///     Returns the amount of frames for <paramref name="milliseconds" /> at specified <paramref name="fps" />.
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double GetTotalMillisecondFrames(double milliseconds, int fps) =>
        GetTotalFrames(milliseconds / 1000.0, fps);

    /// <summary>
    ///     Returns the amount of frames for <paramref name="seconds" /> at specified <paramref name="fps" />.
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double GetTotalFrames(double seconds, int fps) => seconds * fps;

    /// <summary>
    ///     Returns the amount of frames for <paramref name="duration" /> at specified <paramref name="fps" />.
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double GetTotalFrames(TimeSpan duration, int fps) => GetTotalFrames(duration.TotalSeconds, fps);

    /// <summary>
    ///     Returns the amount of frames for <paramref name="seconds" /> at specified <paramref name="fps" />.
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetFrames(double seconds, int fps) => (int)GetTotalFrames(seconds, fps);

    /// <summary>
    ///     Returns the amount of frames for <paramref name="milliseconds" /> at specified <paramref name="fps" />.
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetMillisecondFrames(double milliseconds, int fps) =>
        (int)GetTotalMillisecondFrames(milliseconds, fps);

    /// <summary>
    ///     Returns the amount of frames for <paramref name="duration" /> at specified <paramref name="fps" />.
    /// </summary>
    [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int GetFrames(TimeSpan duration, int fps) => (int)GetTotalFrames(duration, fps);

    /// <summary>
    ///     Frame time calculator
    /// </summary>
    [SuppressMessage("ReSharper", "MemberHidesStaticFromOuterClass")]
    public sealed class Fixed(int frameRate)
    {
        int frameRate = frameRate;

        /// <summary>
        ///     Used frame rate.
        /// </summary>
        public int FrameRate => frameRate;

        /// <summary>Return one frame in seconds</summary>
        public double One
        {
            [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => RateStepSeconds(frameRate);
        }

        /// <summary>Return one frame duration</summary>
        public TimeSpan Step
        {
            [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => RateStep(frameRate);
        }

        /// <summary>
        /// Sets the current <see cref="FrameRate"/>
        /// </summary>
        public void SetFrameRate(int newFps)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(newFps);
            frameRate = newFps;
        }

        /// <summary>
        ///     Returns <see cref="double" /> seconds for <paramref name="frameCount" /> .h
        /// </summary>
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetSeconds(int frameCount) => FrameTime.GetSeconds(frameCount, frameRate);

        /// <summary>
        ///     Returns <see cref="double" /> seconds for <paramref name="frameCount" /> .h
        /// </summary>
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetSeconds(double frameCount) => FrameTime.GetSeconds(frameCount, frameRate);

        /// <summary>
        ///     Returns <see cref="double" /> seconds for <paramref name="frameCount" />.
        /// </summary>
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetMilliseconds(int frameCount) => FrameTime.GetMilliseconds(frameCount, frameRate);

        /// <summary>
        ///     Returns <see cref="System.TimeSpan" /> for <paramref name="frameCount" />.
        /// </summary>
        [Pure]
        public TimeSpan GetDuration(int frameCount) => FrameTime.GetDuration(frameCount, frameRate);

        /// <summary>
        ///     Returns the amount of frames for <paramref name="seconds" />.
        /// </summary>
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetTotalFrames(double seconds) => FrameTime.GetTotalFrames(seconds, frameRate);

        /// <summary>
        ///     Returns the amount of frames for <paramref name="duration" />.
        /// </summary>
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetTotalFrames(TimeSpan duration) => FrameTime.GetTotalFrames(duration, frameRate);

        /// <summary>
        ///     Returns the amount of frames for <paramref name="seconds" />.
        /// </summary>
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetFrames(double seconds) => FrameTime.GetFrames(seconds, frameRate);

        /// <summary>
        ///     Returns the amount of frames for <paramref name="duration" />.
        /// </summary>
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetFrames(TimeSpan duration) => FrameTime.GetFrames(duration, frameRate);

        /// <summary>
        ///     Returns the amount of frames for <paramref name="milliseconds" />.
        /// </summary>
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetMillisecondFrames(double milliseconds) =>
            FrameTime.GetMillisecondFrames(milliseconds, frameRate);

        /// <summary>
        ///     Returns the amount of frames for <paramref name="milliseconds" />.
        /// </summary>
        [Pure, MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetTotalMillisecondFrames(double milliseconds) =>
            FrameTime.GetTotalMillisecondFrames(milliseconds, frameRate);
    }
}

#pragma warning restore S3218, S4136
