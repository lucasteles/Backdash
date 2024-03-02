using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Backdash.Core;

public enum LogLevel : byte
{
    Off = byte.MaxValue,
    Trace = 0,
    Debug,
    Information,
    Warning,
    Error,
}

sealed class Logger : IDisposable
{
    public readonly LogLevel EnabledLevel;

    readonly long start = Stopwatch.GetTimestamp();
    readonly ArrayPool<char> pool = ArrayPool<char>.Shared;

    readonly LogOptions options;
    readonly ILogWriter writer;

    public Logger(
        LogOptions options,
        ILogWriter writer
    )
    {
        this.options = options;
        this.writer = writer;
        EnabledLevel = options.EnabledLevel;
        JobName = $"Log Flush {writer.GetType()}";
    }

    public void Write(LogLevel level, string text) => Write(level, $"{text}");

    public void Write(
        LogLevel level,
        [InterpolatedStringHandlerArgument("", "level")]
        LogInterpolatedStringHandler builder
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
            writer.Write(level, buffer, size);
        }
        finally
        {
            pool.Return(buffer);
        }
    }

    public bool IsEnabledFor(in LogLevel level) => level >= EnabledLevel;

    public static Logger CreateConsoleLogger(LogLevel level) => new(
        new LogOptions
        {
            EnabledLevel = level,
        },
        new ConsoleLogWriter()
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
            LogLevel.Off => throw new InvalidOperationException(),
            _ => "",
        };

    public void Dispose() => writer.Dispose();

    public string JobName { get; }
}
