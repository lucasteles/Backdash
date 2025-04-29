using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Backdash.Options;

namespace Backdash.Core;

/// <summary>
///     Defines logging severity levels.
/// </summary>
public enum LogLevel : byte
{
    /// <summary>
    ///     Specifies that a logging category should not write any messages.
    /// </summary>
    None = byte.MaxValue,

    /// <summary>
    ///     Logs that contain the most detailed messages.
    /// </summary>
    Trace = 0,

    /// <summary>
    ///     Logs that are used for interactive investigation during development.
    /// </summary>
    Debug,

    /// <summary>
    ///     Logs that track the general flow of the application.
    /// </summary>
    Information,

    /// <summary>
    ///     Logs that highlight an abnormal or unexpected event in the application flow,
    ///     but do not otherwise cause the application execution to stop.
    /// </summary>
    Warning,

    /// <summary>
    ///     Logs that highlight when the current flow of execution is stopped due to a failure
    /// </summary>
    Error,
}

sealed class Logger(
    LoggerOptions options,
    ILogWriter writer
) : IDisposable
{
    public readonly LogLevel EnabledLevel = options.EnabledLevel;
    readonly long start = Stopwatch.GetTimestamp();
    readonly ArrayPool<char> pool = ArrayPool<char>.Shared;

    public void Write(LogLevel level, string text) => Write(level, $"{text}");

    public void Write(
        LogLevel level,
        [InterpolatedStringHandlerArgument("", "level")]
        in LogInterpolatedStringHandler builder
    )
    {
        if (!builder.Enabled || !IsEnabledFor(in level)) return;
        ReadOnlySpan<byte> bufferSpan = builder.Buffer;

        var size = Math.Min(builder.Length, bufferSpan.Length);
        var utf8Bytes = bufferSpan[..size];
        var charCount = Encoding.UTF8.GetCharCount(utf8Bytes);
        var buffer = pool.Rent(charCount);

        try
        {
            Encoding.UTF8.GetChars(utf8Bytes, buffer);
            writer.Write(level, buffer, charCount);
        }
        finally
        {
            pool.Return(buffer);
        }
    }

    public void Write(string message, Exception? error = null)
    {
        if (error is null)
            Write(LogLevel.Information, message);
        else
            Write(LogLevel.Error, $"{message} => {error}");
    }

    public bool IsEnabledFor(in LogLevel level) => level >= EnabledLevel;

    public static Logger CreateConsoleLogger(LogLevel level, ILogWriter? writer = null) => new(
        new()
        {
            EnabledLevel = level,
        },
        writer ?? new ConsoleTextLogWriter()
    );

    public void AppendTimestamp(ref LogInterpolatedStringHandler builder)
    {
        if (!options.AppendTimestamps) return;
        var elapsed = Stopwatch.GetElapsedTime(start);
        builder.AppendFormatted(elapsed, options.TimestampFormat);
        builder.AppendFormatted(" "u8);
    }

    public void AppendLevel(in LogLevel level, ref LogInterpolatedStringHandler builder)
    {
        if (!options.AppendLevel) return;
        builder.AppendFormatted(ShortLevelName(level));
        builder.AppendFormatted(": "u8);
    }

    public void AppendThreadId(ref LogInterpolatedStringHandler builder)
    {
        if (!options.AppendThreadId) return;
        builder.AppendFormatted("["u8);
        builder.AppendFormatted(Environment.CurrentManagedThreadId);
        builder.AppendFormatted("] "u8);
    }

    static string ShortLevelName(LogLevel level) =>
        level switch
        {
            LogLevel.Trace => "trc",
            LogLevel.Debug => "dbg",
            LogLevel.Information => "inf",
            LogLevel.Warning => "warn",
            LogLevel.Error => "err",
            LogLevel.None => throw new InvalidOperationException(),
            _ => "",
        };

    public void Dispose() => writer.Dispose();
    public string JobName { get; } = $"Log Flush {writer.GetType()}";
}
